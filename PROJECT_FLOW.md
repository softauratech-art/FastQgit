# FastQ Project Structure and Booking Flow

This document gives the team a quick map of the layers and a concrete booking flow with code snippets for context.

## Layered structure (who owns what)

- FastQ.Web: WebForms UI, API handlers, SignalR hub, and composition root wiring.
- FastQ.Application: use-case services and query services.
- FastQ.Domain: core entities, enums, and repository interfaces.
- FastQ.Infrastructure: in-memory repositories (swap later with DB-backed repos).

## Composition root (where wiring happens)

The Web layer builds up the app by creating repositories and use-case services. This is the single point that connects Web -> Application -> Infrastructure.

```csharp
// src/FastQ.Web/App_Start/CompositionRoot.cs
public static void Initialize()
{
    var repoMode = (WebConfigurationManager.AppSettings["RepositoryMode"] ?? "InMemory").Trim();

    IClock clock = new SystemClock();
    IRealtimeNotifier notifier = new SignalRRealtimeNotifier();

    if (repoMode.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
    {
        var connString = WebConfigurationManager.ConnectionStrings["FastQOracle"]?.ConnectionString;
        Appointments = new OracleAppointmentRepository(connString);
        Customers = new OracleCustomerRepository(connString);
        Queues = new OracleQueueRepository(connString);
        Locations = new OracleLocationRepository(connString);
        Providers = new OracleProviderRepository(connString);
    }
    else
    {
        var store = InMemoryStore.Instance;
        store.EnsureSeeded();

        Appointments = new InMemoryAppointmentRepository(store);
        Customers = new InMemoryCustomerRepository(store);
        Queues = new InMemoryQueueRepository(store);
        Locations = new InMemoryLocationRepository(store);
        Providers = new InMemoryProviderRepository(store);
    }

    Booking = new BookingService(Appointments, Customers, Queues, Locations, clock, notifier);
    Provider = new ProviderService(Appointments, clock, notifier);
    Transfer = new TransferService(Appointments, Queues, clock, notifier);
    SystemClose = new SystemCloseService(Appointments, clock, notifier);

    Queries = new QueueQueryService(Appointments, Customers, Queues, Locations);
}
```

## Repository mode switch (Oracle vs in-memory)

- `RepositoryMode` lives in `src/FastQ.Web/Web.config`.
- `FastQOracle` connection string is the Oracle placeholder.
- Switching to `Oracle` uses DDL-backed repositories in `src/FastQ.Infrastructure/Oracle`.

## ID mapping for DDL alignment

The Oracle schema uses numeric IDs. The app still uses `Guid` IDs in Domain, so repositories map numeric IDs to deterministic GUIDs using `IdMapper`:

```csharp
// src/FastQ.Infrastructure/Common/IdMapper.cs
Guid id = IdMapper.FromLong(11111111);  // numeric -> GUID
IdMapper.TryToLong(id, out long value); // GUID -> numeric
```

This keeps app-layer IDs stable while storing numeric IDs in Oracle.
For demo mode, seeded IDs are generated from numeric keys (see `README.md`).

Oracle repositories follow the provided DDL and fill columns that are not represented in Domain with safe defaults or placeholders (e.g., email).

## Booking flow (layer by layer)

### 1) Web UI sends booking request
The customer page gathers input and posts to the booking handler.

```javascript
// src/FastQ.Web/Customer/Book.aspx (inline script)
$.ajax({
  url: "/Api/Book.ashx",
  method: "POST",
  data: {
    locationId: "00a98ac7-0000-0000-4641-535451494430",
    queueId: this.state.queueId,
    phone: this.state.phone,
    name: this.state.firstName + " " + this.state.lastName,
    smsOptIn: true
  },
  dataType: "json"
})
```

### 2) Web API handler validates request and calls the use-case
The handler reads inputs and delegates to the Application layer.

```csharp
// src/FastQ.Web/Api/Book.ashx.cs
var locId = HandlerUtil.GetGuid(context.Request, "locationId");
var queueId = HandlerUtil.GetGuid(context.Request, "queueId");
var phone = HandlerUtil.GetString(context.Request, "phone");
var name = HandlerUtil.GetString(context.Request, "name");
var smsOptIn = HandlerUtil.GetString(context.Request, "smsOptIn") == "true";

var res = CompositionRoot.Booking.BookFirstAvailable(
    locId.Value, queueId.Value, phone, smsOptIn, name);
```

### 3) Application service enforces business rules
BookingService owns the booking rules and writes appointments via repositories.

```csharp
// src/FastQ.Application/Services/BookingService.cs
if (string.IsNullOrWhiteSpace(phone))
    return Result<Appointment>.Fail("Phone is required.");

var location = _locations.Get(locationId);
if (location == null) return Result<Appointment>.Fail("Location not found.");

var queue = _queues.Get(queueId);
if (queue == null || queue.LocationId != locationId)
    return Result<Appointment>.Fail("Queue not found for this location.");

var upcoming = _appts.ListByCustomer(customer.Id)
    .Count(a => a.Status == AppointmentStatus.Scheduled ||
                a.Status == AppointmentStatus.Arrived ||
                a.Status == AppointmentStatus.InService);

if (upcoming >= queue.Config.MaxUpcomingAppointments)
    return Result<Appointment>.Fail(
        $"Customer already has {upcoming} upcoming appointments (max {queue.Config.MaxUpcomingAppointments}).");
```

### 4) Appointment is created and saved
The appointment is stored through the repository abstraction.

```csharp
// src/FastQ.Application/Services/BookingService.cs
var appt = new Appointment
{
    Id = Guid.NewGuid(),
    LocationId = locationId,
    QueueId = queueId,
    CustomerId = customer.Id,
    ScheduledForUtc = candidate,
    Status = AppointmentStatus.Scheduled,
    CreatedUtc = now,
    UpdatedUtc = now
};

_appts.Add(appt);
```

### 5) Real-time updates are broadcast
The app notifies SignalR groups so other screens can react.

```csharp
// src/FastQ.Web/Realtime/SignalRRealtimeNotifier.cs
Hub.Clients.Group($"loc:{loc}").queueUpdated(loc, q);
Hub.Clients.Group($"queue:{q}").queueUpdated(loc, q);

Hub.Clients.Group($"appt:{apptId}").appointmentUpdated(apptId, appointment.Status.ToString());
```

### 6) Clients receive event and refresh snapshots
The client listens for events and re-fetches snapshots as needed.

```javascript
// src/FastQ.Web/Scripts/fastq.live.js
this.hub.client.queueUpdated = function (locationId, queueId) {
  if (window.onFastQQueueUpdated) {
    window.onFastQQueueUpdated(locationId, queueId);
  }
};

this.hub.client.appointmentUpdated = function (appointmentId, status) {
  if (window.onFastQAppointmentUpdated) {
    window.onFastQAppointmentUpdated(appointmentId, status);
  }
};
```

## In-memory data store (prototype)
The current implementation uses in-memory dictionaries and is designed to be replaced later with a database-backed repository implementation.

```csharp
// src/FastQ.Infrastructure/InMemory/InMemoryStore.cs
public Dictionary<Guid, Appointment> Appointments { get; } = new Dictionary<Guid, Appointment>();

public void EnsureSeeded()
{
    // Seeds demo location/queues/provider using IdMapper.FromLong(...)
}
```

## Where to look next

- Web pages: src/FastQ.Web/Customer and src/FastQ.Web/Provider
- API endpoints: src/FastQ.Web/Api
- Use cases: src/FastQ.Application/Services
- Entities + repositories: src/FastQ.Domain
- In-memory repositories: src/FastQ.Infrastructure/InMemory
- Oracle repositories: src/FastQ.Infrastructure/Oracle

