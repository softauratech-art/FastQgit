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
        private readonly IEntityRepository _entities;
        private readonly IProviderRepository _providers;
        //private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;

        public AdminService()
            : this(
                DbRepositoryFactory.CreateAppointmentRepository(),
                DbRepositoryFactory.CreateCustomerRepository(),
                DbRepositoryFactory.CreateQueueRepository(),
                DbRepositoryFactory.CreateEntityRepository(),
                DbRepositoryFactory.CreateProviderRepository(),
                //new SystemClock(),
                new SignalRRealtimeNotifier())
        {
        }

        public AdminService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            IEntityRepository entities,
            IProviderRepository providers,
            //IClock clock,
            IRealtimeNotifier rt)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            _entities = entities;
            _providers = providers;
            //_clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        public Entity GetCurrentEntity()
        {            
            var sessionEntityId = new AuthService().GetSessionEntityId();            
            return _entities.Get(sessionEntityId);
        }

        public IList<Entity> ListEntities()
        {
            return _entities.ListAll();
        }
        public void UpdateEntity(Entity entity)
        {
            _entities.Update(entity, new AuthService().GetLoggedInWindowsUser());
        }

        public IList<Queue> ListQueuesByEntity(long entityId)
        {
            return ListEligibleQueues(entityId);
        }

        public IList<Queue> ListQueues(long? entityId)
        {
            return ListEligibleQueues(entityId);
        }

        public IList<Customer> ListAllCustomers()
        {
            return _customers.ListAll();
        }

        public IList<Appointment> ListAppointmentsByEntity(long entityId)
        {
            return _appts.ListByEntity(entityId);
        }

        public IList<Provider> ListProviders(long? entityId)
        {
            return entityId.HasValue ? _providers.ListByEntity(entityId.Value) : [];
        }

        public Queue GetQueue(long queueId)
        {
            return _queues.Get(queueId);
        }

        public void UpdateQueue(Queue queue)
        {
            _queues.AddOrUpdateQueue(queue, new AuthService().GetLoggedInWindowsUser());
        }

        //public int CloseStaleScheduledAppointments(int staleHours)
        //{
        //    var now = DateTime.Now;
        //    var cutoff = now.AddHours(-staleHours);

        //    var stale = _appts.ListAll()
        //        .Where(a => a.Status == AppointmentStatus.Scheduled && a.UpdatedOn <= cutoff)
        //        .ToList();

        //    foreach (var a in stale)
        //    {
        //        a.Status = AppointmentStatus.ClosedBySystem;
        //        a.UpdatedOn = now;
        //        a.StampDate = now;
        //        _appts.Update(a);

        //        _rt.AppointmentChanged(a);
        //        _rt.QueueChanged(a.EntityId, a.QueueId);
        //    }

        //    return stale.Count;
        //}

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
