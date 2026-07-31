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

        public static IEntityRepository CreateEntityRepository()
        {
            return new DbEntityRepository();
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
        public static IHolidayRepository CreateHolidayRepository()
        {
            return new DbHolidayRepository();
        }

        public static IWebexRepository CreateWebexRepository()
        {
            return new DbWebexRepository();
        }
        public static IReportingRepository CreateReportingRepository()
        {
            return new DbReportingRepository();
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
