using System;
using System.Configuration;
using FastQ.Data.Repositories;

namespace FastQ.Data.Db
{
    public static class DbRepositoryFactory
    {
        public static IAppointmentRepository CreateAppointmentRepository()
        {
            return new DbAppointmentRepository();
        }

        public static ICustomerRepository CreateCustomerRepository()
        {
            return new DbCustomerRepository();
        }

        public static IQueueRepository CreateQueueRepository()
        {
            return new DbQueueRepository();
        }

        public static ILocationRepository CreateLocationRepository()
        {
            return new DbLocationRepository();
        }

        public static IProviderRepository CreateProviderRepository()
        {
            return new DbProviderRepository();
        }

        public static IServiceTransactionRepository CreateServiceTransactionRepository()
        {
            return new DbServiceTransactionRepository();
        }

        public static IUserRepository CreateUserRepository()
        {
            return new DbUserRepository();
        }
        //private static string GetConnectionString()
        //{
        //    var connString = ConfigurationManager.ConnectionStrings["FastQOracle"]?.ConnectionString;
        //    if (string.IsNullOrWhiteSpace(connString))
        //        throw new InvalidOperationException("FastQ connection string is missing.");

        //    return connString;
        //}
    }
}
