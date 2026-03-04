using System;
using System.Linq;
using FastQ.Data.Common;
using FastQ.Data.Entities;
using FastQ.Data.Db;
using FastQ.Data.Repositories;
using FastQ.Web.Helpers;
using FastQ.Web.Models;

namespace FastQ.Web.Services
{
    public class CustomerService
    {
        private readonly IAppointmentRepository _appts;
        private readonly ICustomerRepository _customers;
        private readonly IQueueRepository _queues;
        private readonly ILocationRepository _locations;
        private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;

        public CustomerService()
            : this(
                DbRepositoryFactory.CreateAppointmentRepository(),
                DbRepositoryFactory.CreateCustomerRepository(),
                DbRepositoryFactory.CreateQueueRepository(),
                DbRepositoryFactory.CreateLocationRepository(),
                new SystemClock(),
                new SignalRRealtimeNotifier())
        {
        }

        public CustomerService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            ILocationRepository locations,
            IClock clock,
            IRealtimeNotifier rt)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            _locations = locations;
            _clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        public Result<Appointment> BookFirstAvailable(long locationId, long queueId, string phone, bool smsOptIn, string name = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Result<Appointment>.Fail("Phone is required.");

            var now = _clock.UtcNow;
            var queue = _queues.Get(queueId);
            if (queue == null) return Result<Appointment>.Fail("Queue not found.");

            if (locationId <= 0)
            {
                locationId = queue.LocationId;
            }

            var location = _locations.Get(locationId);
            if (location == null) return Result<Appointment>.Fail("Location not found.");

            if (queue.LocationId != locationId) return Result<Appointment>.Fail("Queue not found for this location.");

            var customer = _customers.GetByPhone(phone);
            if (customer == null)
            {
                customer = new Customer
                {
                    Id = 0,
                    Phone = phone.Trim(),
                    Name = name,
                    SmsOptIn = smsOptIn,
                    ActiveFlag = true,
                    CreatedUtc = now,
                    UpdatedUtc = now,
                    StampDateUtc = now,
                    StampUser = "web"
                };
                customer.Email = BuildPlaceholderEmail(customer);
                _customers.Add(customer);
            }
            else
            {
                customer.SmsOptIn = smsOptIn;
                if (!string.IsNullOrWhiteSpace(name)) customer.Name = name;
                customer.UpdatedUtc = now;
                customer.StampDateUtc = now;
                _customers.Update(customer);
            }

            var upcoming = _appts.ListByCustomer(customer.Id)
                .Count(a => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Arrived || a.Status == AppointmentStatus.InService);

            if (upcoming >= queue.Config.MaxUpcomingAppointments)
                return Result<Appointment>.Fail($"Customer already has {upcoming} upcoming appointments (max {queue.Config.MaxUpcomingAppointments}).");

            var earliest = now.AddHours(queue.Config.MinHoursLead);
            var latest = now.AddDays(queue.Config.MaxDaysAhead);

            var candidate = new DateTime(earliest.Year, earliest.Month, earliest.Day, earliest.Hour, 0, 0, DateTimeKind.Utc);
            if (candidate < earliest) candidate = candidate.AddHours(1);
            if (candidate > latest) return Result<Appointment>.Fail($"No available slots within {queue.Config.MaxDaysAhead} days.");

            var appt = new Appointment
            {
                Id = 0,
                LocationId = locationId,
                QueueId = queueId,
                CustomerId = customer.Id,
                ScheduledForUtc = candidate,
                Status = AppointmentStatus.Scheduled,
                CreatedBy = "web",
                StampUser = "web",
                CreatedOnUtc = now,
                StampDateUtc = now,
                CreatedUtc = now,
                UpdatedUtc = now
            };

            _appts.Add(appt);

            _rt.AppointmentChanged(appt, appt.CreatedBy);
            _rt.QueueChanged(locationId, queueId, appt.CreatedBy);

            return Result<Appointment>.Success(appt);
        }

        public Result<Appointment> CreateScheduled(
            long queueId,
            string serviceId,
            string refValue,
            string customerName,
            string phone,
            string contactType,
            DateTime scheduledForUtc,
            string notes,
            string meetingUrl,
            string stampUser)
        {
            if (queueId <= 0)
                return Result<Appointment>.Fail("Queue is required.");
            if (string.IsNullOrWhiteSpace(customerName))
                return Result<Appointment>.Fail("Customer name is required.");
            if (string.IsNullOrWhiteSpace(phone))
                return Result<Appointment>.Fail("Phone is required.");

            var queue = _queues.Get(queueId);
            if (queue == null) return Result<Appointment>.Fail("Queue not found.");

            var now = _clock.UtcNow;
            var user = string.IsNullOrWhiteSpace(stampUser) ? "web" : stampUser.Trim();
            var customer = GetOrCreateCustomer(customerName, phone, !string.IsNullOrWhiteSpace(meetingUrl), user, now);

            var appt = new Appointment
            {
                Id = 0,
                LocationId = queue.LocationId,
                QueueId = queueId,
                CustomerId = customer.Id,
                ServiceId = long.TryParse(serviceId, out var parsedServiceId) ? parsedServiceId : (long?)null,
                RefCriteria = string.IsNullOrWhiteSpace(refValue) ? null : refValue.Trim(),
                RefValue = string.IsNullOrWhiteSpace(refValue) ? null : refValue.Trim(),
                ContactType = string.IsNullOrWhiteSpace(contactType) ? "IP" : contactType.Trim(),
                MoreInfo = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim(),
                Status = AppointmentStatus.Scheduled,
                CreatedBy = user,
                StampUser = user,
                CreatedOnUtc = now,
                StampDateUtc = now,
                CreatedUtc = now,
                UpdatedUtc = now
            };
            appt.ScheduledForUtc = scheduledForUtc;

            _appts.Add(appt);
            _rt.AppointmentChanged(appt, user);
            _rt.QueueChanged(appt.LocationId, appt.QueueId, user);

            return Result<Appointment>.Success(appt);
        }

        public Result<long> CreateWalkin(
            long queueId,
            string serviceId,
            string refValue,
            string customerName,
            string phone,
            string contactType,
            string notes,
            string stampUser)
        {
            if (queueId <= 0)
                return Result<long>.Fail("Queue is required.");
            if (string.IsNullOrWhiteSpace(customerName))
                return Result<long>.Fail("Customer name is required.");
            if (string.IsNullOrWhiteSpace(phone))
                return Result<long>.Fail("Phone is required.");

            var queue = _queues.Get(queueId);
            if (queue == null) return Result<long>.Fail("Queue not found.");

            var now = _clock.UtcNow;
            var user = string.IsNullOrWhiteSpace(stampUser) ? "web" : stampUser.Trim();
            var customer = GetOrCreateCustomer(customerName, phone, false, user, now);

            var walkin = new Appointment
            {
                Id = 0,
                LocationId = queue.LocationId,
                QueueId = queueId,
                CustomerId = customer.Id,
                ServiceId = long.TryParse(serviceId, out var parsedServiceId) ? parsedServiceId : (long?)null,
                RefCriteria = string.IsNullOrWhiteSpace(refValue) ? null : refValue.Trim(),
                RefValue = string.IsNullOrWhiteSpace(refValue) ? null : refValue.Trim(),
                ContactType = string.IsNullOrWhiteSpace(contactType) ? "IP" : contactType.Trim(),
                MoreInfo = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                Status = AppointmentStatus.Arrived,
                CreatedBy = user,
                StampUser = user,
                CreatedOnUtc = now,
                StampDateUtc = now,
                CreatedUtc = now,
                UpdatedUtc = now
            };
            walkin.ScheduledForUtc = now;

            var newId = _appts.AddWalkin(walkin);
            _rt.AppointmentChanged(walkin, user);
            _rt.QueueChanged(walkin.LocationId, walkin.QueueId, user);

            return Result<long>.Success(newId);
        }

        public Result Cancel(long appointmentId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return Result.Fail("Appointment not found.");

            if (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.ClosedBySystem)
                return Result.Fail("Appointment cannot be cancelled.");

            appt.Status = AppointmentStatus.Cancelled;
            appt.UpdatedUtc = _clock.UtcNow;
            appt.StampDateUtc = appt.UpdatedUtc;
            _appts.Update(appt);

            _rt.AppointmentChanged(appt, appt.StampUser);
            _rt.QueueChanged(appt.LocationId, appt.QueueId, appt.StampUser);

            return Result.Success();
        }

        public AppointmentSnapshotDto GetAppointmentSnapshot(long appointmentId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return null;

            var location = _locations.Get(appt.LocationId);
            var queue = _queues.Get(appt.QueueId);

            var snapshot = new AppointmentSnapshotDto
            {
                AppointmentId = appt.Id,
                LocationId = appt.LocationId,
                QueueId = appt.QueueId,
                LocationName = location?.Name ?? "Unknown",
                QueueName = queue?.Name ?? "Unknown",
                Status = appt.Status.ToString(),
                ScheduledForUtc = appt.ScheduledForUtc.ToString("u"),
                UpdatedUtc = appt.UpdatedUtc.ToString("u"),
            };

            bool IsWaiting(Appointment a) => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Arrived;

            var waiting = _appts.ListByQueue(appt.QueueId)
                .Where(IsWaiting)
                .OrderBy(a => a.CreatedUtc)
                .ToList();

            snapshot.WaitingCount = waiting.Count;

            var idx = waiting.FindIndex(a => a.Id == appt.Id);
            if (idx >= 0) snapshot.PositionInQueue = idx + 1;

            return snapshot;
        }

        private Customer GetOrCreateCustomer(string name, string phone, bool smsOptIn, string stampUser, DateTime now)
        {
            var customer = _customers.GetByPhone(phone);
            if (customer == null)
            {
                customer = new Customer
                {
                    Id = 0,
                    Phone = phone.Trim(),
                    Name = (name ?? string.Empty).Trim(),
                    SmsOptIn = smsOptIn,
                    ActiveFlag = true,
                    CreatedUtc = now,
                    UpdatedUtc = now,
                    StampDateUtc = now,
                    StampUser = stampUser
                };
                customer.Email = BuildPlaceholderEmail(customer);
                _customers.Add(customer);
                return customer;
            }

            customer.SmsOptIn = smsOptIn;
            if (!string.IsNullOrWhiteSpace(name))
            {
                customer.Name = name.Trim();
            }
            customer.Phone = phone.Trim();
            customer.UpdatedUtc = now;
            customer.StampDateUtc = now;
            customer.StampUser = stampUser;
            _customers.Update(customer);
            return customer;
        }

        private static string BuildPlaceholderEmail(Customer customer)
        {
            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                return customer.Email;
            }

            if (!string.IsNullOrWhiteSpace(customer.Phone))
            {
                return $"{customer.Phone}@placeholder.local";
            }

            var first = customer.FirstName ?? "customer";
            var last = customer.LastName ?? "unknown";
            return $"{first}.{last}@placeholder.local".Replace(" ", string.Empty).ToLowerInvariant();
        }
    }
}
