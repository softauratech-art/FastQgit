using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Common;
using FastQ.Data.Entities;
using FastQ.Data.Db;
using FastQ.Data.Repositories;
using FastQ.Web.Helpers;
using FastQ.Web.Models;
using Newtonsoft.Json.Linq;

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
            if (!long.TryParse(serviceId, out var parsedServiceId) || parsedServiceId <= 0)
                return Result<Appointment>.Fail("Service is required.");

            var queue = _queues.Get(queueId);
            if (queue == null) return Result<Appointment>.Fail("Queue not found.");

            var validation = ValidateScheduledInputAgainstQueueDetails(
                queueId,
                parsedServiceId,
                contactType,
                refValue,
                scheduledForUtc);
            if (!validation.Ok)
                return Result<Appointment>.Fail(validation.Error);

            var now = _clock.UtcNow;
            var user = string.IsNullOrWhiteSpace(stampUser) ? "web" : stampUser.Trim();
            var customer = GetOrCreateCustomer(customerName, phone, !string.IsNullOrWhiteSpace(meetingUrl), user, now);

            var appt = new Appointment
            {
                Id = 0,
                LocationId = queue.LocationId,
                QueueId = queueId,
                CustomerId = customer.Id,
                CustomerEmail = customer.Email,
                ServiceId = parsedServiceId,
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
            var insertedAppt = _appts.Get(appt.Id) ?? appt;
            _rt.AppointmentChanged(insertedAppt, user);
            _rt.QueueChanged(insertedAppt.LocationId, insertedAppt.QueueId, user);

            return Result<Appointment>.Success(insertedAppt);
        }

        public Result<long> CreateWalkin(
            long queueId,
            string serviceId,
            string refValue,
            string customerName,
            string phone,
            string contactType,
            string meetingUrl,
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
                CustomerEmail = customer.Email,
                ServiceId = long.TryParse(serviceId, out var parsedServiceId) ? parsedServiceId : (long?)null,
                RefCriteria = string.IsNullOrWhiteSpace(refValue) ? null : refValue.Trim(),
                RefValue = string.IsNullOrWhiteSpace(refValue) ? null : refValue.Trim(),
                ContactType = string.IsNullOrWhiteSpace(contactType) ? "IP" : contactType.Trim(),
                MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim(),
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
            if (string.IsNullOrWhiteSpace(customer.Email))
            {
                customer.Email = BuildPlaceholderEmail(customer);
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

        private Result ValidateScheduledInputAgainstQueueDetails(long queueId, long serviceId, string contactType, string refValue, DateTime scheduledForUtc)
        {
            var jsonParts = _queues.GetQueueDetailsJson(queueId);
            if (jsonParts == null)
                return Result.Fail("Queue details not found.");

            try
            {
                var servicesJson = ParseJsonObject(jsonParts.Item1);
                var schedulesJson = ParseJsonObject(jsonParts.Item2);
                var detailsJson = ParseJsonObject(jsonParts.Item3);

                var serviceCodes = ReadOptionCodes(servicesJson?["services"], "service_id");
                if (serviceCodes.Count > 0 && !serviceCodes.Contains(serviceId.ToString()))
                    return Result.Fail("Selected service is not valid for this queue.");

                var contactCode = (contactType ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(contactCode))
                {
                    var contactCodes = ReadOptionCodes(detailsJson?["contactoptions"], "type_key");
                    if (contactCodes.Count > 0 && !contactCodes.Contains(contactCode))
                        return Result.Fail("Selected contact type is not valid for this queue.");
                }

                var refCode = (refValue ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(refCode))
                {
                    var refCodes = ReadOptionCodes(detailsJson?["refoptions"], "ref_key");
                    if (refCodes.Count > 0 && !refCodes.Contains(refCode))
                        return Result.Fail("Selected reference is not valid for this queue.");
                }

                if (!IsScheduledSlotAllowed(schedulesJson?["schedules"], scheduledForUtc))
                    return Result.Fail("Selected date/time is not valid for this queue schedule.");
            }
            catch
            {
                return Result.Fail("Could not validate queue details.");
            }

            return Result.Success();
        }

        private static JObject ParseJsonObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            return JObject.Parse(json);
        }

        private static HashSet<string> ReadOptionCodes(JToken listToken, string codeField)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = listToken as JArray;
            if (list == null)
                return set;

            foreach (var item in list.OfType<JObject>())
            {
                var code = item[codeField]?.ToString();
                if (!string.IsNullOrWhiteSpace(code))
                    set.Add(code.Trim());
            }
            return set;
        }

        private static bool IsScheduledSlotAllowed(JToken schedulesToken, DateTime scheduledForUtc)
        {
            var schedules = schedulesToken as JArray;
            if (schedules == null || schedules.Count == 0)
                return false;

            var local = scheduledForUtc.Kind == DateTimeKind.Utc ? scheduledForUtc.ToLocalTime() : scheduledForUtc;
            var date = local.Date;
            var minutes = (local.Hour * 60) + local.Minute;
            var weekdayCode = date.DayOfWeek == DayOfWeek.Sunday ? "7" : ((int)date.DayOfWeek).ToString();

            foreach (var row in schedules.OfType<JObject>())
            {
                var begin = ParseDateOnly(row["date_begin"]?.ToString());
                var end = ParseDateOnly(row["date_end"]?.ToString());
                if (begin.HasValue && date < begin.Value)
                    continue;
                if (end.HasValue && date > end.Value)
                    continue;

                var weekly = (row["weekly_sch"]?.ToString() ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(weekly) && !weekly.Contains(weekdayCode))
                    continue;

                var open = ParseIsoDurationMinutes(row["open_time"]?.ToString());
                var close = ParseIsoDurationMinutes(row["close_time"]?.ToString());
                var interval = ParseIsoDurationMinutes(row["interval_time"]?.ToString());
                if (interval <= 0)
                    interval = 30;

                var normalizedWindow = NormalizeScheduleWindow(open, close);
                if (!normalizedWindow.HasValue)
                    continue;

                for (var slot = normalizedWindow.Value.Open; slot < normalizedWindow.Value.Close; slot += interval)
                {
                    if (slot == minutes)
                        return true;
                }
            }

            return false;
        }

        private static DateTime? ParseDateOnly(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (DateTime.TryParse(value, out var parsed))
                return parsed.Date;

            return null;
        }

        private static int ParseIsoDurationMinutes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;
            var text = value.Trim().ToUpperInvariant();
            if (!text.StartsWith("P"))
                return 0;

            var total = 0;
            var cursor = 1;
            var inTime = false;
            var number = string.Empty;
            while (cursor < text.Length)
            {
                var ch = text[cursor++];
                if (ch == 'T')
                {
                    inTime = true;
                    number = string.Empty;
                    continue;
                }
                if (char.IsDigit(ch))
                {
                    number += ch;
                    continue;
                }
                if (string.IsNullOrEmpty(number))
                    return 0;

                var part = int.Parse(number);
                number = string.Empty;
                if (ch == 'D')
                    total += part * 24 * 60;
                else if (ch == 'H' && inTime)
                    total += part * 60;
                else if (ch == 'M' && inTime)
                    total += part;
                else
                    return 0;
            }
            return total;
        }

        private static ScheduleWindow? NormalizeScheduleWindow(int open, int close)
        {
            const int dayMinutes = 24 * 60;
            if (open < 0 || close < 0)
                return null;

            if (close <= open)
                close += 12 * 60;
            if (close <= open)
                close += 12 * 60;

            if (close > dayMinutes)
                close = dayMinutes;
            if (close <= open)
                return null;

            return new ScheduleWindow { Open = open, Close = close };
        }

        private struct ScheduleWindow
        {
            public int Open;
            public int Close;
        }
    }
}
