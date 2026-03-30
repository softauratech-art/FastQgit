using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Entities;
using FastQ.Data.Db;
using FastQ.Data.Repositories;
using FastQ.Web.Helpers;

namespace FastQ.Web.Services
{
    public class AdminService
    {
        private readonly IAppointmentRepository _appts;
        private readonly ICustomerRepository _customers;
        private readonly IQueueRepository _queues;
        // readonly ILocationRepository _locations;
        private readonly IProviderRepository _providers;
        private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;

        public AdminService()
            : this(
                DbRepositoryFactory.CreateAppointmentRepository(),
                DbRepositoryFactory.CreateCustomerRepository(),
                DbRepositoryFactory.CreateQueueRepository(),
                //DbRepositoryFactory.CreateLocationRepository(),
                DbRepositoryFactory.CreateProviderRepository(),
                new SystemClock(),
                new SignalRRealtimeNotifier())
        {
        }

        public AdminService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            //ILocationRepository locations,
            IProviderRepository providers,
            IClock clock,
            IRealtimeNotifier rt)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            //_locations = locations;
            _providers = providers;
            _clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        //public Location GetPrimaryLocation()
        //{
        //    return _locations.ListAll().FirstOrDefault();
        //}

        //public IList<Location> ListLocations()
        //{
        //    return _locations.ListAll();
        //}

        public IList<Queue> ListQueuesByLocation(long locationId)
        {
            return ListEligibleQueues(locationId);
        }

        public IList<Queue> ListQueues(long? locationId)
        {
            return ListEligibleQueues(locationId);
        }

        public IList<Customer> ListAllCustomers()
        {
            return _customers.ListAll();
        }

        public IList<Appointment> ListAppointmentsByLocation(long locationId)
        {
            return _appts.ListByLocation(locationId);
        }

        public IList<Provider> ListProviders(long? locationId)
        {
            return locationId.HasValue ? _providers.ListByEntity(locationId.Value) : [];
        }

        public Queue GetQueue(long queueId)
        {
            return _queues.Get(queueId);
        }

        public void UpdateQueue(Queue queue)
        {
            _queues.AddOrUpdateQueue(queue, new AuthService().GetLoggedInWindowsUser());
        }

        public int CloseStaleScheduledAppointments(int staleHours)
        {
            var now = _clock.UtcNow;
            var cutoff = now.AddHours(-staleHours);

            var stale = _appts.ListAll()
                .Where(a => a.Status == AppointmentStatus.Scheduled && a.UpdatedUtc <= cutoff)
                .ToList();

            foreach (var a in stale)
            {
                a.Status = AppointmentStatus.ClosedBySystem;
                a.UpdatedUtc = now;
                a.StampDateUtc = now;
                _appts.Update(a);

                _rt.AppointmentChanged(a);
                _rt.QueueChanged(a.LocationId, a.QueueId);
            }

            return stale.Count;
        }

        private IList<Queue> ListEligibleQueues(long? requestedEntityId = null)
        {
            var auth = new AuthService();
            var sessionEntityId = auth.GetSessionEntityId();
            var effectiveEntityId = sessionEntityId > 0
                ? (long?)sessionEntityId
                : (requestedEntityId.HasValue && requestedEntityId.Value > 0 ? requestedEntityId : (long?)null);

            if (!effectiveEntityId.HasValue || effectiveEntityId.Value <= 0)
            {
                return new List<Queue>();
            }

            return _queues.ListByEntity(effectiveEntityId.Value, auth.GetLoggedInWindowsUser())
                .Where(q => q != null && q.ActiveFlag && !q.EmpOnly)
                .OrderBy(q => q.Name)
                .ToList();
        }
    }
}
