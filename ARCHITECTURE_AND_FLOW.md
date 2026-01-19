# FastQ Architecture + Project Flow (Full)

This document is the end-to-end explanation of how the simplified Web + Data project works, with code references.

## Architecture (Simplified)

### FastQ.Web (UI + business logic)
- WebForms pages (`.aspx`) and code-behind (`.aspx.cs`).
- Business rules live in code-behind or Web services.
- PageMethods enable async refresh without API handlers.
- SignalR provides live updates + toast notifications.

### FastQ.Data (Entities + storage)
- Entities and enums.
- Repository interfaces + implementations.
- InMemory store (default) and Oracle repositories (optional).

Dependency direction: `Web -> Data` only.

## Composition Root (Wiring)

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

## Page Flow (WebForms like MVC)

### Step 1: User opens a page
- Example: `src/FastQ.Web/Customer/Book.aspx`.

### Step 2: User submits a form (postback)
- Code-behind runs business logic and calls Data layer.

```csharp
// src/FastQ.Web/Customer/Book.aspx.cs
protected void CreateAppointment_Click(object sender, EventArgs e)
{
    // read inputs, validate
    var res = CompositionRoot.Booking.BookFirstAvailable(DefaultLocationId, queueId, phone, true, name);
    // handle result + redirect
}
```

### Step 2b: Async refresh via PageMethods
- JavaScript calls code-behind methods directly (no API handlers).

```csharp
// src/FastQ.Web/Customer/Status.aspx.cs
[WebMethod]
[ScriptMethod(ResponseFormat = ResponseFormat.Json)]
public static object GetAppointmentSnapshot(string appointmentId)
{
    var dto = CompositionRoot.Queries.GetAppointmentSnapshot(apptId);
    return new { ok = true, data = dto };
}
```

### Step 3: Data layer reads/writes storage
- Uses InMemory repositories by default.

```csharp
// src/FastQ.Data/InMemory/InMemoryStore.cs
public Dictionary<Guid, Appointment> Appointments { get; } = new Dictionary<Guid, Appointment>();
```

### Step 4: SignalR pushes live updates
- Every appointment change triggers a broadcast and a 3-second toast.

```csharp
// src/FastQ.Web/Realtime/SignalRRealtimeNotifier.cs
Hub.Clients.Group($"queue:{q}").appointmentUpdated(apptId, appointment.Status.ToString());
Hub.Clients.All.notify("Customer arrived (...)");
```

## Oracle ID Mapping

```csharp
// src/FastQ.Data/Common/IdMapper.cs
Guid id = IdMapper.FromLong(11111111);  // number -> GUID
IdMapper.TryToLong(id, out long value); // GUID -> number
```

## Where to Debug
- Pages + code-behind: `src/FastQ.Web/Customer`, `src/FastQ.Web/Provider`, `src/FastQ.Web/Admin`, `src/FastQ.Web/Reporting`
- Data layer: `src/FastQ.Data`
- InMemory storage: `src/FastQ.Data/InMemory`
- Oracle repositories: `src/FastQ.Data/Oracle`
- SignalR client: `src/FastQ.Web/Scripts/fastq.live.js`
