using System;
using System.Configuration;
using FastQ.Data.Repositories;

namespace FastQ.Data.Oracle
{
    public static class OracleRepositoryFactory
    {
        public static IAppointmentRepository CreateAppointmentRepository()
        {
            return new OracleAppointmentRepository(GetConnectionString());
        }

        public static ICustomerRepository CreateCustomerRepository()
        {
            return new OracleCustomerRepository(GetConnectionString());
        }

        public static IQueueRepository CreateQueueRepository()
        {
            return new OracleQueueRepository(GetConnectionString());
        }

        public static ILocationRepository CreateLocationRepository()
        {
            return new OracleLocationRepository(GetConnectionString());
        }

        public static IProviderRepository CreateProviderRepository()
        {
            return new OracleProviderRepository(GetConnectionString());
        }

        public static IServiceTransactionRepository CreateServiceTransactionRepository()
        {
            return new OracleServiceTransactionRepository(GetConnectionString());
        }

        private static string GetConnectionString()
        {
            var connString = ConfigurationManager.ConnectionStrings["FastQOracle"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connString))
                throw new InvalidOperationException("FastQOracle connection string is missing.");

            return connString;
        }
    }
}
