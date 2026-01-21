# Architecture (Simplified)

## 1) FastQ.Web (UI + business logic)
**Responsibility**
- MVC controllers + Razor views (`.cshtml`)
- Controller-level business rules and validation
- Composition root that wires repositories + services
- SignalR Hub + live notifications
- JSON controller actions for async UI refresh (no API handlers)

**Depends on:** FastQ.Data

## 2) FastQ.Data (Entities + storage)
**Responsibility**
- Entities and enums (Appointment, Customer, Queue, AppointmentStatus)
- Repository interfaces + implementations
- In-memory storage (default)
- Oracle repositories (optional)

**No dependencies** on Web.

---

# Live Queue Handling with SignalR (customer -> provider)

## Server-side pattern
1. Controller or service updates data (e.g., booking, arrive, begin, end, transfer).
2. `SignalRRealtimeNotifier` broadcasts:
   - `queueUpdated(locationId, queueId)`
   - `appointmentUpdated(appointmentId, status)`
   - `notify(message)` (3-second toast to all clients)

## Client-side pattern (push + pull)
- **Push:** SignalR events arrive immediately.
- **Pull:** Pages call MVC JSON actions to fetch fresh snapshots (no `/Api` handlers).

## Where it's implemented
- Hub: `src/FastQ.Web/Hubs/QueueHub.cs`
- Notifier: `src/FastQ.Web/Realtime/SignalRRealtimeNotifier.cs`
- Client: `src/FastQ.Web/Scripts/fastq.live.js`
- Controllers:
  - `src/FastQ.Web/Controllers/CustomerController.cs`
  - `src/FastQ.Web/Controllers/ProviderController.cs`
  - `src/FastQ.Web/Controllers/AdminController.cs`
  - `src/FastQ.Web/Controllers/ReportingController.cs`
