using System;
using System.Web.Configuration;
using FastQ.Application.Abstractions;
using FastQ.Application.Notifications;
using FastQ.Application.Queries;
using FastQ.Application.Services;
using FastQ.Domain.Repositories;
using FastQ.Infrastructure.InMemory;
using FastQ.Infrastructure.Oracle;
using FastQ.Web.Realtime;

namespace FastQ.Web.App_Start
{
    public static class CompositionRoot
    {
        // Repos
        public static IAppointmentRepository Appointments { get; private set; }
        public static ICustomerRepository Customers { get; private set; }
        public static IQueueRepository Queues { get; private set; }
        public static ILocationRepository Locations { get; private set; }
        public static IProviderRepository Providers { get; private set; }

        // Services
        public static BookingService Booking { get; private set; }
        public static ProviderService Provider { get; private set; }
        public static TransferService Transfer { get; private set; }
        public static SystemCloseService SystemClose { get; private set; }

        // Queries
        public static QueueQueryService Queries { get; private set; }

        public static void Initialize()
        {
            var repoMode = (WebConfigurationManager.AppSettings["RepositoryMode"] ?? "InMemory").Trim();

            IClock clock = new SystemClock();
            IRealtimeNotifier notifier = new SignalRRealtimeNotifier();

            if (repoMode.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
            {
                var connString = WebConfigurationManager.ConnectionStrings["FastQOracle"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connString))
                    throw new InvalidOperationException("FastQOracle connection string is missing.");

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
    }
}
