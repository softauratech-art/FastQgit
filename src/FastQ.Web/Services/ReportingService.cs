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
        private readonly IReportingRepository _reports;

        public ReportingService()
            : this(
                DbRepositoryFactory.CreateAppointmentRepository(),
                DbRepositoryFactory.CreateProviderRepository(),
                DbRepositoryFactory.CreateQueueRepository(),
                DbRepositoryFactory.CreateReportingRepository())
        {
        }

        public ReportingService(IAppointmentRepository appts, IProviderRepository providers, IQueueRepository queues, IReportingRepository reports)
        {
            _appts = appts;
            _providers = providers;
            _queues = queues;
            _reports = reports;
        }

        public IList<Appointment> ListAppointments(long? entityId)
        {
            return entityId.HasValue ? _appts.ListByEntity(entityId.Value) : null;
        }

        public IList<ProviderAppointmentData> ListAppointmentsWalkins(long? entityId, string userId, DateTime startDate, DateTime endDate)
        {
            if (!entityId.HasValue)
                return new List<ProviderAppointmentData>();

            var walkins = _appts.ListWalkinsForUser(entityId.Value, userId, startDate, endDate);
            var appointments = _appts.ListForUser(entityId.Value, userId, startDate, endDate);
            return walkins.Concat(appointments).ToList();
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

        public IList<QueueLengthsReport> ListQueueLengths(DateTime startDate, DateTime endDate, string granularity, long? queueId, string srcType)
        {
            var auth = new AuthService();
            return _reports.GetQueueLengths(auth.GetSessionEntityId(), startDate, endDate, granularity, queueId, srcType, auth.GetLoggedInWindowsUser())
                .OrderBy(q => q.Queue_Name)
                .ToList();
        }

        public IList<AverageServiceDurationReport> ListServiceDurations(DateTime startDate, DateTime endDate, string granularity, long? queueId)
        {
            var auth = new AuthService();
            return _reports.GetServiceDurations(auth.GetSessionEntityId(), startDate, endDate, granularity, queueId, auth.GetLoggedInWindowsUser())
                .OrderBy(q => q.Queue_Name)
                .ToList();
        }
    }
}
