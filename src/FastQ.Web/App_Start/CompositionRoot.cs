using FastQ.Application.Abstractions;
using FastQ.Application.Notifications;
using FastQ.Application.Queries;
using FastQ.Application.Services;
using FastQ.Infrastructure.InMemory;
using FastQ.Web.Realtime;

namespace FastQ.Web.App_Start
{
    public static class CompositionRoot
    {
        public static InMemoryStore Store { get; private set; }

        // Repos
        public static InMemoryAppointmentRepository Appointments { get; private set; }
        public static InMemoryCustomerRepository Customers { get; private set; }
        public static InMemoryQueueRepository Queues { get; private set; }
        public static InMemoryLocationRepository Locations { get; private set; }
        public static InMemoryProviderRepository Providers { get; private set; }

        // Services
        public static BookingService Booking { get; private set; }
        public static ProviderService Provider { get; private set; }
        public static TransferService Transfer { get; private set; }
        public static SystemCloseService SystemClose { get; private set; }

        // Queries
        public static QueueQueryService Queries { get; private set; }

        public static void Initialize()
        {
            Store = InMemoryStore.Instance;
            Store.EnsureSeeded();

            Appointments = new InMemoryAppointmentRepository(Store);
            Customers = new InMemoryCustomerRepository(Store);
            Queues = new InMemoryQueueRepository(Store);
            Locations = new InMemoryLocationRepository(Store);
            Providers = new InMemoryProviderRepository(Store);

            IClock clock = new SystemClock();
            IRealtimeNotifier notifier = new SignalRRealtimeNotifier();

            Booking = new BookingService(Appointments, Customers, Queues, Locations, clock, notifier);
            Provider = new ProviderService(Appointments, clock, notifier);
            Transfer = new TransferService(Appointments, Queues, clock, notifier);
            SystemClose = new SystemCloseService(Appointments, clock, notifier);

            Queries = new QueueQueryService(Appointments, Customers, Queues, Locations);
        }
    }
}
