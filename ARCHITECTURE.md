# Architecture (Layered)

## 1) FastQ.Domain (Business Core)
**Responsibility**
- Entities (Appointment, Customer, Queue, Location)
- Enums (AppointmentStatus)
- Repository interfaces (ports)
- Result / Result<T>

**No dependencies** on other layers.

## 2) FastQ.Application (Use cases)
**Responsibility**
- Use-case services (BookingService, ProviderService, TransferService, SystemCloseService)
- Query services (QueueQueryService) for UI snapshots
- Abstractions:
  - `IClock` for testable time
  - `IRealtimeNotifier` to publish changes (SignalR implementation lives in Web)

**Depends on:** Domain (entities + repo interfaces)

## 3) FastQ.Infrastructure (Adapters)
**Responsibility**
- In-memory data store + repository implementations
- Designed to be replaced later:
  - Swap `InMemory*Repository` → `Sql*Repository` or EF repositories
  - No changes required in Domain/Application if interfaces stay the same

**Depends on:** Domain

## 4) FastQ.Web (Presentation + real-time)
**Responsibility**
- WebForms pages (Customer/Book, Customer/Status, Provider/Today)
- API handlers (.ashx) returning JSON snapshots
- SignalR Hub (`QueueHub`) + OWIN Startup
- `SignalRRealtimeNotifier` implements `IRealtimeNotifier`:
  - broadcasts to groups:
    - `loc:{locationId}`
    - `queue:{queueId}`
    - `appt:{appointmentId}`

**Depends on:** Application + Domain + Infrastructure

---

# Live Queue Handling with SignalR (customer ↔ provider)

## Server-side pattern
1. A use case modifies data (Application service)
2. It calls `IRealtimeNotifier`:
   - `AppointmentChanged(appointment)`
   - `QueueChanged(locationId, queueId)`
3. SignalR broadcasts lightweight events:
   - `queueUpdated(locationId, queueId)`
   - `appointmentUpdated(appointmentId, status)`

## Client-side pattern (push + pull)
- **Push**: SignalR event arrives immediately
- **Pull**: client calls the relevant snapshot endpoint to refresh UI
  - `/Api/QueueSnapshot.ashx?locationId=...&queueId=...`
  - `/Api/AppointmentSnapshot.ashx?appointmentId=...`

This avoids syncing complex UI state through SignalR and keeps the hub payloads small and reliable.

## Where it’s implemented
- Hub: `src/FastQ.Web/Hubs/QueueHub.cs`
- Notifier: `src/FastQ.Web/Realtime/SignalRRealtimeNotifier.cs`
- Client: `src/FastQ.Web/Scripts/fastq.live.js`
- Snapshots:
  - `src/FastQ.Web/Api/QueueSnapshot.ashx(.cs)`
  - `src/FastQ.Web/Api/AppointmentSnapshot.ashx(.cs)`
