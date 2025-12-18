using System;
using System.Linq;
using FastQ.Application.Abstractions;
using FastQ.Application.Notifications;
using FastQ.Domain.Entities;
using FastQ.Domain.Repositories;

namespace FastQ.Application.Services
{
    // Manual "system-close" job (prototype): closes scheduled appointments that are too old.
    public class SystemCloseService
    {
        private readonly IAppointmentRepository _appts;
        private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;

        public SystemCloseService(IAppointmentRepository appts, IClock clock, IRealtimeNotifier rt = null)
        {
            _appts = appts;
            _clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        public int CloseStaleScheduledAppointments(int staleHours = 12)
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
                _appts.Update(a);

                _rt.AppointmentChanged(a);
                _rt.QueueChanged(a.LocationId, a.QueueId);
            }

            return stale.Count;
        }
    }
}
