using System;
using FastQ.Web.Models;

namespace FastQ.Web.Services
{
    public class SharedService
    {
        private readonly QueueQueryService _queries;
        private readonly SystemCloseService _systemClose;

        public SharedService()
            : this(new QueueQueryService(), new SystemCloseService())
        {
        }

        public SharedService(QueueQueryService queries, SystemCloseService systemClose)
        {
            _queries = queries;
            _systemClose = systemClose;
        }

        public QueueSnapshotDto GetQueueSnapshot(Guid locationId, Guid queueId)
        {
            return _queries.GetQueueSnapshot(locationId, queueId);
        }

        public AppointmentSnapshotDto GetAppointmentSnapshot(Guid appointmentId)
        {
            return _queries.GetAppointmentSnapshot(appointmentId);
        }

        public int CloseStaleScheduledAppointments(int staleHours)
        {
            return _systemClose.CloseStaleScheduledAppointments(staleHours);
        }
    }
}
