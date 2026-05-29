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
        private readonly IRealtimeNotifier _rt;

        public AdminService()
            : this(
                DbRepositoryFactory.CreateAppointmentRepository(),
                DbRepositoryFactory.CreateCustomerRepository(),
                DbRepositoryFactory.CreateQueueRepository(),
                DbRepositoryFactory.CreateEntityRepository(),
                DbRepositoryFactory.CreateProviderRepository(),
                new SignalRRealtimeNotifier())
        {
        }

        public AdminService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            IEntityRepository entities,
            IProviderRepository providers,
            IRealtimeNotifier rt)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            _entities = entities;
            _providers = providers;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        public Entity GetCurrentEntity()
        {            
            var sessionEntityId = new AuthService().GetSessionEntityId();            
            return _entities.Get(sessionEntityId);
        }

        public void UpdateEntity(Entity entity)
        {
            _entities.Update(entity, new AuthService().GetLoggedInWindowsUser());
        }
    }
}
