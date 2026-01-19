# FastQ Project Flow (Explained Like You Are Brand New)

This is the "slow, simple" version. It tells you what runs first, what talks to what, and where to look when something breaks. Read it top to bottom once, then use it as a map.

## Big picture (what lives where)

Think of the app as two boxes. Know which box you are in before you edit code.

- FastQ.Web: WebForms pages, code-behind logic, wiring, and business rules.
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
SignalR still runs so provider and customer screens stay in sync.

Example references (file + line):
- `src/FastQ.Web/App_Start/CompositionRoot.cs:32` Initialize entry point.
- `src/FastQ.Web/App_Start/CompositionRoot.cs:39` Oracle branch.
- `src/FastQ.Web/App_Start/CompositionRoot.cs:53` In-memory branch.
- `src/FastQ.Web/App_Start/CompositionRoot.cs:63` Services wired.

## Repository mode switch (Oracle vs in-memory)

- `RepositoryMode` is in `src/FastQ.Web/Web.config`.
- `InMemory` is the default.
- `Oracle` uses real DB repositories in `src/FastQ.Data/Oracle`.

If you switch to Oracle and it breaks, check connection string name `FastQOracle` first.

Example references (file + line):
- `src/FastQ.Web/Web.config:5` RepositoryMode flag.
- `src/FastQ.Web/Web.config:9` FastQOracle connection string.

## ID mapping for Oracle

Oracle tables use numbers. The app uses `Guid`. So repositories convert numbers to GUIDs in a repeatable way.

```csharp
// src/FastQ.Data/Common/IdMapper.cs
Guid id = IdMapper.FromLong(11111111);  // number -> GUID
IdMapper.TryToLong(id, out long value); // GUID -> number
```

This is how the app keeps IDs stable in code while Oracle stores numbers.

Example references (file + line):
- `src/FastQ.Data/Common/IdMapper.cs:9` FromLong (number -> GUID).
- `src/FastQ.Data/Common/IdMapper.cs:17` TryToLong (GUID -> number).

## Page flow (simple MVC-style WebForms)

Think of each `.aspx` page as the "view" and its code-behind (`.aspx.cs`) as the "controller + business logic". The code-behind directly calls the Data layer to read/write data.

### Step 1: User opens a page
The browser requests a page like `Customer/Book.aspx`.

What this means in plain English:
- The page loads.
- Code-behind can load data needed for dropdowns or defaults.

Example references (file + line):
- `src/FastQ.Web/Customer/Book.aspx` Page markup.
- `src/FastQ.Web/Customer/Book.aspx.cs` Code-behind logic.

### Step 2: User clicks a button (postback)
The page posts back to its own code-behind event handler (no API handlers).

What this means in plain English:
- Code-behind reads form fields.
- Code-behind runs the business rules.
- Code-behind calls the Data layer to save.

Example references (file + line):
- `src/FastQ.Web/Customer/Book.aspx.cs` Event handlers and server-side logic.

### Step 2b: PageMethods for async refresh
Pages can call `PageMethods` (static methods in code-behind) to fetch snapshots without full reloads.

What this means in plain English:
- JavaScript calls a code-behind method directly.
- Code-behind reads data and returns JSON.

Example references (file + line):
- `src/FastQ.Web/Customer/Status.aspx.cs` PageMethods for appointment snapshot.
- `src/FastQ.Web/Provider/Today.aspx.cs` PageMethods for queue snapshot and actions.

### Step 3: Data layer reads/writes storage
The code-behind calls repositories (in-memory or Oracle).

What this means in plain English:
- If `RepositoryMode=InMemory`, data is stored in dictionaries.
- If `RepositoryMode=Oracle`, data is stored in Oracle tables.

Example references (file + line):
- `src/FastQ.Data/InMemory/InMemoryStore.cs` In-memory storage.
- `src/FastQ.Data/InMemory/InMemoryAppointmentRepository.cs` In-memory data access.
- `src/FastQ.Data/Oracle/OracleAppointmentRepository.cs` Oracle data access.

### Step 4: SignalR pushes live updates
Whenever an appointment changes, SignalR broadcasts updates and a short toast message to all clients.

What this means in plain English:
- Providers and customers see changes instantly.
- No refresh is needed to receive notifications.

Example references (file + line):
- `src/FastQ.Web/Realtime/SignalRRealtimeNotifier.cs` Sends updates + toast notifications.
- `src/FastQ.Web/Scripts/fastq.live.js` Receives updates and shows 3‑second toasts.

## In-memory store (prototype mode)

Right now, data is kept in memory. It is reset when the app restarts.

```csharp
// src/FastQ.Data/InMemory/InMemoryStore.cs
public Dictionary<Guid, Appointment> Appointments { get; } = new Dictionary<Guid, Appointment>();

public void EnsureSeeded()
{
    // Seeds demo location/queues/provider using IdMapper.FromLong(...)
}
```

If you expect data to persist, you need Oracle repos instead.

What this means in plain English:
- All data sits in dictionaries in memory.
- The seed method builds a demo location/queues/provider.

Example references (file + line):
- `src/FastQ.Data/InMemory/InMemoryStore.cs:16` In-memory dictionaries.
- `src/FastQ.Data/InMemory/InMemoryStore.cs:45` Seed data method.

## Where to look when debugging

- Web pages + code-behind: `src/FastQ.Web/Customer`, `src/FastQ.Web/Provider`
- Business logic: `src/FastQ.Web/Customer/*.aspx.cs`, `src/FastQ.Web/Provider/*.aspx.cs`
- Entities + repos: `src/FastQ.Data`
- In-memory repos: `src/FastQ.Data/InMemory`
- Oracle repos: `src/FastQ.Data/Oracle`

