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

        public IList<Appointment> ListAppointments(long? locationId)
        {
            return locationId.HasValue ? _appts.ListByLocation(locationId.Value) : _appts.ListAll();
        }

        public IList<Provider> ListProviders(long? locationId)
        {
            return locationId.HasValue ? _providers.ListByEntity(locationId.Value) : [];
        }

        public IList<Queue> ListQueues(long? locationId)
        {
            var auth = new AuthService();
            var sessionEntityId = auth.GetSessionEntityId();
            var effectiveEntityId = sessionEntityId > 0
                ? (long?)sessionEntityId
                : (locationId.HasValue && locationId.Value > 0 ? locationId : (long?)null);

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
