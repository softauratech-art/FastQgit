# FastQ Architecture + Project Flow (Full)

This document is the end-to-end explanation of how the MVC Web + Data project works, with code references.

## Architecture (Simplified)

### FastQ.Web (UI + business logic)
- MVC controllers and Razor views (`.cshtml`).
- Business rules live in controller actions or app services.
- JSON controller actions enable async refresh (no API handlers).
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

## MVC Page Flow (End-to-End)

### Step 1: User opens a page
- Example view: `src/FastQ.Web/Views/Customer/Book.cshtml`.
- Controller action: `src/FastQ.Web/Controllers/CustomerController.cs` (`Book()` GET).

### Step 2: User submits a form
- Form posts to an MVC action that runs business rules and calls Data.

```csharp
// src/FastQ.Web/Controllers/CustomerController.cs
[HttpPost]
public ActionResult Book(BookForm form)
{
    var res = CompositionRoot.Booking.BookFirstAvailable(
        form.LocationId, form.QueueId, form.Phone, form.SmsOptIn, form.Name);

    if (!res.Ok)
    {
        ModelState.AddModelError("", res.Error);
        return View(form);
    }

    return RedirectToAction("Status", new { appointmentId = res.Value.Id });
}
```

### Step 3: Data layer reads/writes storage
- InMemory repositories are used by default.

```csharp
// src/FastQ.Data/InMemory/InMemoryStore.cs
public Dictionary<Guid, Appointment> Appointments { get; } = new Dictionary<Guid, Appointment>();
```

### Step 4: SignalR pushes live updates
- Every appointment change triggers a broadcast + toast.

```csharp
// src/FastQ.Web/Realtime/SignalRRealtimeNotifier.cs
Hub.Clients.Group($"queue:{q}").appointmentUpdated(apptId, appointment.Status.ToString());
Hub.Clients.All.notify("Customer arrived (...)");
```

### Step 5: Views refresh via JSON actions
- JavaScript calls JSON endpoints to refresh without full reloads.

```csharp
// src/FastQ.Web/Controllers/ProviderController.cs
[HttpGet]
public JsonResult GetQueueSnapshot(Guid locationId, Guid queueId)
{
    var dto = CompositionRoot.Queries.GetQueueSnapshot(locationId, queueId);
    return Json(new { ok = true, data = dto }, JsonRequestBehavior.AllowGet);
}
```

## Oracle ID Mapping

```csharp
// src/FastQ.Data/Common/IdMapper.cs
Guid id = IdMapper.FromLong(11111111);  // number -> GUID
IdMapper.TryToLong(id, out long value); // GUID -> number
```

## Where to Debug
- Controllers: `src/FastQ.Web/Controllers`
- Views: `src/FastQ.Web/Views`
- Business logic: `src/FastQ.Web/App`
- Data layer: `src/FastQ.Data`
- InMemory storage: `src/FastQ.Data/InMemory`
- Oracle repositories: `src/FastQ.Data/Oracle`
- SignalR client: `src/FastQ.Web/Scripts/fastq.live.js`
