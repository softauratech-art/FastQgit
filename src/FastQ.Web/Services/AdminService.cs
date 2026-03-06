using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Entities;
using FastQ.Data.Db;
using FastQ.Data.Repositories;

namespace FastQ.Web.Services
{
    public class AdminService
    {
        private readonly IAppointmentRepository _appts;
        private readonly ICustomerRepository _customers;
        private readonly IQueueRepository _queues;
        private readonly ILocationRepository _locations;

        public AdminService()
            : this(
                DbRepositoryFactory.CreateAppointmentRepository(),
                DbRepositoryFactory.CreateCustomerRepository(),
                DbRepositoryFactory.CreateQueueRepository(),
                DbRepositoryFactory.CreateLocationRepository())
        {
        }

        public AdminService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            ILocationRepository locations)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            _locations = locations;
        }

        public Location GetPrimaryLocation()
        {
            return _locations.ListAll().FirstOrDefault();
        }

        public IList<Queue> ListQueuesByLocation(long locationId)
        {
            //return _queues.ListByLocation(locationId);
            return _queues.ListByEntity(locationId, new AuthService().GetLoggedInWindowsUser());
        }

        public IList<Customer> ListAllCustomers()
        {
            return _customers.ListAll();
        }

        public IList<Appointment> ListAppointmentsByLocation(long locationId)
        {
            return _appts.ListByLocation(locationId);
        }

    }
}
