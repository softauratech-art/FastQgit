# Architecture (Simplified)

## 1) FastQ.Web (UI + business logic)
**Responsibility**
- WebForms pages (`.aspx`) and code-behind (`.aspx.cs`)
- Page-level business rules and validation
- Composition root that wires repositories + services
- SignalR Hub + live notifications
- PageMethods (AJAX) for async UI refresh without API handlers

**Depends on:** FastQ.Data

## 2) FastQ.Data (Entities + storage)
**Responsibility**
- Entities and enums (Appointment, Customer, Queue, AppointmentStatus)
- Repository interfaces + implementations
- In-memory storage (default)
- Oracle repositories (optional)

**No dependencies** on Web.

---

# Live Queue Handling with SignalR (customer ? provider)

## Server-side pattern
1. Code-behind or service updates data (e.g., booking, arrive, begin, end, transfer).
2. `SignalRRealtimeNotifier` broadcasts:
   - `queueUpdated(locationId, queueId)`
   - `appointmentUpdated(appointmentId, status)`
   - `notify(message)` (3-second toast to all clients)

## Client-side pattern (push + pull)
- **Push:** SignalR events arrive immediately.
- **Pull:** Pages call PageMethods to fetch fresh snapshots (no `/Api` handlers).

## Where it’s implemented
- Hub: `src/FastQ.Web/Hubs/QueueHub.cs`
- Notifier: `src/FastQ.Web/Realtime/SignalRRealtimeNotifier.cs`
- Client: `src/FastQ.Web/Scripts/fastq.live.js`
- PageMethods:
  - `src/FastQ.Web/Customer/Status.aspx.cs`
  - `src/FastQ.Web/Customer/Home.aspx.cs`
  - `src/FastQ.Web/Provider/Today.aspx.cs`
  - `src/FastQ.Web/Admin/Dashboard.aspx.cs`
  - `src/FastQ.Web/Reporting/Overview.aspx.cs`
