using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Common;
using FastQ.Data.Entities;
using FastQ.Data.Oracle;
using FastQ.Data.Repositories;
using FastQ.Web.Helpers;
using FastQ.Web.Models;

namespace FastQ.Web.Services
{
    public class ProviderControllerService
    {
        private readonly IAppointmentRepository _appts;
        private readonly ICustomerRepository _customers;
        private readonly IQueueRepository _queues;
        private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;
        private readonly SharedService _shared;

        public ProviderControllerService()
            : this(
                OracleRepositoryFactory.CreateAppointmentRepository(),
                OracleRepositoryFactory.CreateCustomerRepository(),
                OracleRepositoryFactory.CreateQueueRepository(),
                new SystemClock(),
                new SignalRRealtimeNotifier(),
                new SharedService())
        {
        }

        public ProviderControllerService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            IClock clock,
            IRealtimeNotifier rt,
            SharedService shared)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            _clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
            _shared = shared;
        }

        public IList<Queue> ListQueues()
        {
            return _queues.ListAll();
        }

        public IList<Customer> ListCustomers()
        {
            return _customers.ListAll();
        }

        public IList<Appointment> ListAppointmentsForDate(DateTime utcDate)
        {
            var date = utcDate.Date;
            return _appts.ListAll()
                .Where(a => a.ScheduledForUtc.Date == date)
                .ToList();
        }

        public QueueSnapshotDto GetQueueSnapshot(Guid locationId, Guid queueId)
        {
            return _shared.GetQueueSnapshot(locationId, queueId);
        }

        public Result HandleProviderAction(string action, Guid appointmentId, Guid providerId)
        {
            action = (action ?? string.Empty).Trim().ToLowerInvariant();
            return action switch
            {
                "arrive" => MarkArrived(appointmentId),
                "begin" => BeginService(appointmentId, providerId),
                "end" => EndService(appointmentId),
                _ => Result.Fail("Unknown action")
            };
        }

        public Result<Appointment> TransferAppointment(Guid appointmentId, Guid targetQueueId)
        {
            return Transfer(appointmentId, targetQueueId);
        }

        public int CloseStaleScheduledAppointments(int staleHours)
        {
            return _shared.CloseStaleScheduledAppointments(staleHours);
        }

        private Result MarkArrived(Guid appointmentId)
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

        private Result BeginService(Guid appointmentId, Guid providerId)
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

        private Result EndService(Guid appointmentId)
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

        private Result<Appointment> Transfer(Guid appointmentId, Guid targetQueueId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return Result<Appointment>.Fail("Appointment not found.");

            var targetQueue = _queues.Get(targetQueueId);
            if (targetQueue == null) return Result<Appointment>.Fail("Target queue not found.");

            if (targetQueue.LocationId != appt.LocationId)
                return Result<Appointment>.Fail("Transfer must be within the same location.");

            if (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.ClosedBySystem)
                return Result<Appointment>.Fail("Cannot transfer a finished appointment.");

            appt.Status = AppointmentStatus.TransferredOut;
            appt.UpdatedUtc = _clock.UtcNow;
            appt.StampDateUtc = appt.UpdatedUtc;
            _appts.Update(appt);

            var newAppt = new Appointment
            {
                Id = Guid.NewGuid(),
                LocationId = appt.LocationId,
                QueueId = targetQueueId,
                CustomerId = appt.CustomerId,
                ScheduledForUtc = _clock.UtcNow,
                Status = AppointmentStatus.Arrived,
                CreatedBy = "web",
                StampUser = "web",
                CreatedOnUtc = _clock.UtcNow,
                StampDateUtc = _clock.UtcNow,
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

        public IList<ProviderAppointmentRow> BuildRows(
            IList<Appointment> appointments,
            IDictionary<Guid, Queue> queueMap,
            IDictionary<Guid, Customer> customerMap)
        {
            return appointments.Select(a =>
            {
                queueMap.TryGetValue(a.QueueId, out var queue);
                customerMap.TryGetValue(a.CustomerId, out var customer);

                var contact = customer != null && customer.SmsOptIn ? "Online" : "In-Person";

                return new ProviderAppointmentRow
                {
                    AppointmentId = a.Id,
                    ScheduledForUtc = a.ScheduledForUtc,
                    StartTimeText = a.ScheduledForUtc.ToString("h:mm tt"),
                    StartDateText = a.ScheduledForUtc.ToString("MMM dd, yyyy"),
                    QueueName = queue?.Name ?? "Unknown Queue",
                    ServiceType = queue?.Name != null ? $"Questions: {queue.Name}" : "Questions: General",
                    CustomerName = customer?.Name ?? "Unknown",
                    Phone = customer?.Phone ?? "-",
                    Status = a.Status,
                    StatusText = a.Status.ToString().ToUpperInvariant(),
                    ContactMethod = contact
                };
            }).OrderBy(r => r.ScheduledForUtc).ToList();
        }
    }
}
