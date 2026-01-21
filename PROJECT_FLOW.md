# FastQ Project Flow (MVC, Simplified)

This is the "slow, simple" version. It tells you what runs first, what talks to what, and where to look when something breaks.

## Big picture (what lives where)

Think of the app as two boxes. Know which box you are in before you edit code.

- FastQ.Web: MVC controllers, Razor views, SignalR, wiring, and business rules.
- FastQ.Data: entities, repository interfaces, and repository implementations (in-memory + Oracle).

Rule of thumb:
- Web owns the rules and calls Data.
- Data owns storage and never calls back into Web.

## Composition root (the wiring place)

All the objects are created in one place. That file decides which repository implementation to use (in-memory or Oracle) and connects everything together.

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

If something fails because "repo is null" or "service is null", this is the first file to check.

## MVC page flow (one example)

### Step 1: The browser opens a view
- Example view: `src/FastQ.Web/Views/Customer/Book.cshtml`.
- Controller action: `src/FastQ.Web/Controllers/CustomerController.cs` (`Book()` GET).

### Step 2: User submits a form (POST)
The form posts to an MVC action that owns the business rule.

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
The Data layer handles persistence (in-memory by default).

```csharp
// src/FastQ.Data/InMemory/InMemoryStore.cs
public Dictionary<Guid, Appointment> Appointments { get; } = new Dictionary<Guid, Appointment>();
```

### Step 4: SignalR pushes live updates
Every status change triggers SignalR broadcasts + a toast for all clients.

```csharp
// src/FastQ.Web/Realtime/SignalRRealtimeNotifier.cs
Hub.Clients.Group($"queue:{q}").appointmentUpdated(apptId, appointment.Status.ToString());
Hub.Clients.All.notify("Customer arrived (...)");
```

### Step 5: Pages refresh via JSON actions
Views call MVC JSON actions to refresh without full page reloads.

```csharp
// src/FastQ.Web/Controllers/CustomerController.cs
[HttpGet]
public JsonResult GetAppointmentSnapshot(string appointmentId)
{
    var dto = CompositionRoot.Queries.GetAppointmentSnapshot(Guid.Parse(appointmentId));
    return Json(new { ok = true, data = dto }, JsonRequestBehavior.AllowGet);
}
```

## Repository mode switch (Oracle vs in-memory)

- `RepositoryMode` is in `src/FastQ.Web/Web.config`.
- `InMemory` is the default.
- `Oracle` uses real DB repositories in `src/FastQ.Data/Oracle`.

If you switch to Oracle and it breaks, check connection string name `FastQOracle` first.

## ID mapping for Oracle

Oracle tables use numbers. The app uses `Guid`. So repositories convert numbers to GUIDs in a repeatable way.

```csharp
// src/FastQ.Data/Common/IdMapper.cs
Guid id = IdMapper.FromLong(11111111);  // number -> GUID
IdMapper.TryToLong(id, out long value); // GUID -> number
```

## Where to look when debugging

- Controllers: `src/FastQ.Web/Controllers`
- Views: `src/FastQ.Web/Views`
- Business logic: `src/FastQ.Web/App`
- Data layer: `src/FastQ.Data`
- In-memory repos: `src/FastQ.Data/InMemory`
- Oracle repos: `src/FastQ.Data/Oracle`
- SignalR client: `src/FastQ.Web/Scripts/fastq.live.js`
