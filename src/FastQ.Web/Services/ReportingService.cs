using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Entities;
using FastQ.Data.Db;
using FastQ.Data.Repositories;

namespace FastQ.Web.Services
{
    public class ReportingService
    {
        private readonly IAppointmentRepository _appts;
        private readonly IProviderRepository _providers;
        private readonly IQueueRepository _queues;

        public ReportingService()
            : this(
                DbRepositoryFactory.CreateAppointmentRepository(),
                DbRepositoryFactory.CreateProviderRepository(),
                DbRepositoryFactory.CreateQueueRepository())
        {
        }

        public ReportingService(IAppointmentRepository appts, IProviderRepository providers, IQueueRepository queues)
        {
            _appts = appts;
            _providers = providers;
            _queues = queues;
        }

        public IList<Appointment> ListAppointments(long? entityId)
        {
            return entityId.HasValue ? _appts.ListByEntity(entityId.Value) : null;
        }

        public IList<Provider> ListProviders(long? entityId)
        {
            return entityId.HasValue ? _providers.ListByEntity(entityId.Value) : [];
        }

        public IList<Queue> ListQueues(long? entityId)
        {
            var auth = new AuthService();
            var sessionEntityId = auth.GetSessionEntityId();
            var effectiveEntityId = sessionEntityId > 0
                ? (long?)sessionEntityId
                : (entityId.HasValue && entityId.Value > 0 ? entityId : (long?)null);

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
