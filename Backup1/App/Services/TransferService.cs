using System;
using FastQ.Web.Abstractions;
using FastQ.Web.Notifications;
using FastQ.Data.Common;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Web.Services
{
    public class TransferService
    {
        private readonly IAppointmentRepository _appts;
        private readonly IQueueRepository _queues;
        private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;

        public TransferService(IAppointmentRepository appts, IQueueRepository queues, IClock clock, IRealtimeNotifier rt = null)
        {
            _appts = appts;
            _queues = queues;
            _clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        // Transfer within same location: end current appointment + create new arrived appointment in target queue
        public Result<Appointment> Transfer(Guid appointmentId, Guid targetQueueId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return Result<Appointment>.Fail("Appointment not found.");

            var targetQueue = _queues.Get(targetQueueId);
            if (targetQueue == null) return Result<Appointment>.Fail("Target queue not found.");

            if (targetQueue.LocationId != appt.LocationId)
                return Result<Appointment>.Fail("Transfer must be within the same location.");

            if (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.ClosedBySystem)
                return Result<Appointment>.Fail("Cannot transfer a finished appointment.");

            // mark old as transferred out
            appt.Status = AppointmentStatus.TransferredOut;
            appt.UpdatedUtc = _clock.UtcNow;
            _appts.Update(appt);

            var newAppt = new Appointment
            {
                Id = Guid.NewGuid(),
                LocationId = appt.LocationId,
                QueueId = targetQueueId,
                CustomerId = appt.CustomerId,
                ScheduledForUtc = _clock.UtcNow,
                Status = AppointmentStatus.Arrived,
                CreatedUtc = _clock.UtcNow,
                UpdatedUtc = _clock.UtcNow
            };
            _appts.Add(newAppt);

            _rt.AppointmentChanged(appt);
            _rt.QueueChanged(appt.LocationId, appt.QueueId);

            _rt.AppointmentChanged(newAppt);
            _rt.QueueChanged(newAppt.LocationId, newAppt.QueueId);

            return Result<Appointment>.Success(newAppt);
        }
    }
}

