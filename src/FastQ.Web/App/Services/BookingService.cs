using System;
using System.Linq;
using FastQ.Web.Abstractions;
using FastQ.Web.Notifications;
using FastQ.Data.Common;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Web.Services
{
    public class BookingService
    {
        private readonly IAppointmentRepository _appts;
        private readonly ICustomerRepository _customers;
        private readonly IQueueRepository _queues;
        private readonly ILocationRepository _locations;
        private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;

        public BookingService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            ILocationRepository locations,
            IClock clock,
            IRealtimeNotifier rt = null)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            _locations = locations;
            _clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        public Result<Appointment> BookFirstAvailable(Guid locationId, Guid queueId, string phone, bool smsOptIn, string name = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Result<Appointment>.Fail("Phone is required.");

            var location = _locations.Get(locationId);
            if (location == null) return Result<Appointment>.Fail("Location not found.");

            var queue = _queues.Get(queueId);
            if (queue == null || queue.LocationId != locationId) return Result<Appointment>.Fail("Queue not found for this location.");

            var customer = _customers.GetByPhone(phone);
            if (customer == null)
            {
                customer = new FastQ.Data.Entities.Customer
                {
                    Id = Guid.NewGuid(),
                    Phone = phone.Trim(),
                    Name = name,
                    SmsOptIn = smsOptIn,
                    CreatedUtc = _clock.UtcNow,
                    UpdatedUtc = _clock.UtcNow
                };
                _customers.Add(customer);
            }
            else
            {
                customer.SmsOptIn = smsOptIn;
                if (!string.IsNullOrWhiteSpace(name)) customer.Name = name;
                customer.UpdatedUtc = _clock.UtcNow;
                _customers.Update(customer);
            }

            // rule: max upcoming appointments
            var upcoming = _appts.ListByCustomer(customer.Id)
                .Count(a => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Arrived || a.Status == AppointmentStatus.InService);

            if (upcoming >= queue.Config.MaxUpcomingAppointments)
                return Result<Appointment>.Fail($"Customer already has {upcoming} upcoming appointments (max {queue.Config.MaxUpcomingAppointments}).");

            // rule: min lead time, max 30 days ahead
            var now = _clock.UtcNow;
            var earliest = now.AddHours(queue.Config.MinHoursLead);
            var latest = now.AddDays(queue.Config.MaxDaysAhead);

            // first available (prototype): next whole hour >= earliest
            var candidate = new DateTime(earliest.Year, earliest.Month, earliest.Day, earliest.Hour, 0, 0, DateTimeKind.Utc);
            if (candidate < earliest) candidate = candidate.AddHours(1);
            if (candidate > latest) return Result<Appointment>.Fail($"No available slots within {queue.Config.MaxDaysAhead} days.");

            var appt = new Appointment
            {
                Id = Guid.NewGuid(),
                LocationId = locationId,
                QueueId = queueId,
                CustomerId = customer.Id,
                ScheduledForUtc = candidate,
                Status = AppointmentStatus.Scheduled,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            _appts.Add(appt);

            _rt.AppointmentChanged(appt);
            _rt.QueueChanged(locationId, queueId);

            return Result<Appointment>.Success(appt);
        }

        public Result Cancel(Guid appointmentId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return Result.Fail("Appointment not found.");

            if (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.ClosedBySystem)
                return Result.Fail("Appointment cannot be cancelled.");

            appt.Status = AppointmentStatus.Cancelled;
            appt.UpdatedUtc = _clock.UtcNow;
            _appts.Update(appt);

            _rt.AppointmentChanged(appt);
            _rt.QueueChanged(appt.LocationId, appt.QueueId);

            return Result.Success();
        }
    }
}

