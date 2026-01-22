using System;
using FastQ.Web.Helpers;
using FastQ.Web.Services;
using FastQ.Data.Common;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Web.Services
{
    public class ProviderService
    {
        private readonly IAppointmentRepository _appts;
        private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;

        public ProviderService(IAppointmentRepository appts, IClock clock, IRealtimeNotifier rt = null)
        {
            _appts = appts;
            _clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        public Result MarkArrived(Guid appointmentId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return Result.Fail("Appointment not found.");

            if (appt.Status != AppointmentStatus.Scheduled)
                return Result.Fail("Only scheduled appointments can be marked arrived.");

            appt.Status = AppointmentStatus.Arrived;
            appt.UpdatedUtc = _clock.UtcNow;
            appt.StampDateUtc = appt.UpdatedUtc;
            _appts.Update(appt);

            _rt.AppointmentChanged(appt);
            _rt.QueueChanged(appt.LocationId, appt.QueueId);

            return Result.Success();
        }

        public Result BeginService(Guid appointmentId, Guid providerId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return Result.Fail("Appointment not found.");

            if (appt.Status != AppointmentStatus.Arrived && appt.Status != AppointmentStatus.Scheduled)
                return Result.Fail("Appointment must be scheduled or arrived to begin service.");

            appt.Status = AppointmentStatus.InService;
            appt.ProviderId = providerId;
            appt.UpdatedUtc = _clock.UtcNow;
            appt.StampDateUtc = appt.UpdatedUtc;
            _appts.Update(appt);

            _rt.AppointmentChanged(appt);
            _rt.QueueChanged(appt.LocationId, appt.QueueId);

            return Result.Success();
        }

        public Result EndService(Guid appointmentId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return Result.Fail("Appointment not found.");

            if (appt.Status != AppointmentStatus.InService)
                return Result.Fail("Appointment must be in service to end service.");

            appt.Status = AppointmentStatus.Completed;
            appt.UpdatedUtc = _clock.UtcNow;
            appt.StampDateUtc = appt.UpdatedUtc;
            _appts.Update(appt);

            _rt.AppointmentChanged(appt);
            _rt.QueueChanged(appt.LocationId, appt.QueueId);

            return Result.Success();
        }
    }
}


