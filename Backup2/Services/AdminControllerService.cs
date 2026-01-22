using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Web.Services
{
    public class AdminControllerService
    {
        private readonly AdminService _admin;
        private readonly SharedService _shared;

        public AdminControllerService()
            : this(new AdminService(), new SharedService())
        {
        }

        public AdminControllerService(AdminService admin, SharedService shared)
        {
            _admin = admin;
            _shared = shared;
        }

        public IList<Location> ListLocations()
        {
            return _admin.ListLocations();
        }

        public IList<Queue> ListQueuesByLocation(Guid locationId)
        {
            return _admin.ListQueuesByLocation(locationId);
        }

        public IList<Customer> ListAllCustomers()
        {
            return _admin.ListAllCustomers();
        }

        public IList<Appointment> ListAppointmentsByLocation(Guid locationId)
        {
            return _admin.ListAppointmentsByLocation(locationId);
        }

        public IList<Queue> ListQueues(Guid? locationId)
        {
            return _admin.ListQueues(locationId);
        }

        public IList<Provider> ListProviders(Guid? locationId)
        {
            return _admin.ListProviders(locationId);
        }

        public Queue GetQueue(Guid queueId)
        {
            return _admin.GetQueue(queueId);
        }

        public void UpdateQueue(Queue queue)
        {
            _admin.UpdateQueue(queue);
        }

        public int CloseStaleScheduledAppointments(int staleHours)
        {
            return _shared.CloseStaleScheduledAppointments(staleHours);
        }
    }
}
