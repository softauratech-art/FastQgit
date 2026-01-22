using System;
using System.Web.Configuration;
using FastQ.Web.Helpers;
using FastQ.Web.Services;
using FastQ.Data.Repositories;
using FastQ.Data.Oracle;

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
        public static ProviderScheduleService ProviderSchedule { get; private set; }
        public static TransferService Transfer { get; private set; }
        public static SystemCloseService SystemClose { get; private set; }
        public static AdminService Admin { get; private set; }
        public static ReportingService Reporting { get; private set; }

        // Queries
        public static QueueQueryService Queries { get; private set; }

        public static void Initialize()
        {
            IClock clock = new SystemClock();
            IRealtimeNotifier notifier = new SignalRRealtimeNotifier();

            var connString = WebConfigurationManager.ConnectionStrings["FastQOracle"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connString))
                throw new InvalidOperationException("FastQOracle connection string is missing.");

            Appointments = new OracleAppointmentRepository(connString);
            Customers = new OracleCustomerRepository(connString);
            Queues = new OracleQueueRepository(connString);
            Locations = new OracleLocationRepository(connString);
            Providers = new OracleProviderRepository(connString);
            Booking = new BookingService(Appointments, Customers, Queues, Locations, clock, notifier);
            Provider = new ProviderService(Appointments, clock, notifier);
            ProviderSchedule = new ProviderScheduleService(Appointments, Customers, Queues);
            Transfer = new TransferService(Appointments, Queues, clock, notifier);
            SystemClose = new SystemCloseService(Appointments, clock, notifier);
            Admin = new AdminService(Appointments, Customers, Queues, Locations, Providers);
            Reporting = new ReportingService(Appointments, Providers, Queues);

            Queries = new QueueQueryService(Appointments, Customers, Queues, Locations);
        }
    }
}



