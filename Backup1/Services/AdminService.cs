using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Web.Services
{
    public class AdminService
    {
        private readonly IAppointmentRepository _appts;
        private readonly ICustomerRepository _customers;
        private readonly IQueueRepository _queues;
        private readonly ILocationRepository _locations;
        private readonly IProviderRepository _providers;

        public AdminService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            ILocationRepository locations,
            IProviderRepository providers)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            _locations = locations;
            _providers = providers;
        }

        public Location GetPrimaryLocation()
        {
            return _locations.ListAll().FirstOrDefault();
        }

        public IList<Location> ListLocations()
        {
            return _locations.ListAll();
        }

        public IList<Queue> ListQueuesByLocation(Guid locationId)
        {
            return _queues.ListByLocation(locationId);
        }

        public IList<Queue> ListQueues(Guid? locationId)
        {
            return locationId.HasValue ? _queues.ListByLocation(locationId.Value) : _queues.ListAll();
        }

        public IList<Customer> ListAllCustomers()
        {
            return _customers.ListAll();
        }

        public IList<Appointment> ListAppointmentsByLocation(Guid locationId)
        {
            return _appts.ListByLocation(locationId);
        }

        public IList<Provider> ListProviders(Guid? locationId)
        {
            return locationId.HasValue ? _providers.ListByLocation(locationId.Value) : _providers.ListAll();
        }

        public Queue GetQueue(Guid queueId)
        {
            return _queues.Get(queueId);
        }

        public void UpdateQueue(Queue queue)
        {
            _queues.Update(queue);
        }
    }
}
