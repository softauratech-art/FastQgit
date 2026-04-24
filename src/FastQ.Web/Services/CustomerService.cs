using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using FastQ.Data.Common;
using FastQ.Data.Entities;
using FastQ.Data.Db;
using FastQ.Data.Repositories;
using FastQ.Web.Helpers;
using FastQ.Web.Models;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;

namespace FastQ.Web.Services
{
    public class CustomerService
    {
        private readonly IAppointmentRepository _appts;
        private readonly ICustomerRepository _customers;
        private readonly IQueueRepository _queues;
        //private readonly ILocationRepository _locations;
        private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;

        public CustomerService()
            : this(
                DbRepositoryFactory.CreateAppointmentRepository(),
                DbRepositoryFactory.CreateCustomerRepository(),
                DbRepositoryFactory.CreateQueueRepository(),
                //DbRepositoryFactory.CreateLocationRepository(),
                new SystemClock(),
                new SignalRRealtimeNotifier())
        {
        }

        public CustomerService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            //ILocationRepository locations,
            IClock clock,
            IRealtimeNotifier rt)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            //_locations = locations;
            _clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        public Result<Appointment> CreateScheduled(
            long queueId,
            string serviceId,
            string refValue,
            string permitNumber,
            string streetNumber,
            string streetName,
            string streetType,
            string email,
            string customerName,
            string phone,
            string contactType,
            DateTime scheduledFor,
            string languagePreference,
            string notes,
            string meetingUrl,
            string stampUser)
        {
            var refCriteria = string.IsNullOrWhiteSpace(refValue) ? null : refValue.Trim();
            var referenceValue = BuildReferenceValue(refCriteria, permitNumber, streetNumber, streetName, streetType);

            if (queueId <= 0)
                return Result<Appointment>.Fail("Queue is required.");
            if (string.IsNullOrWhiteSpace(referenceValue))
                return Result<Appointment>.Fail("Reference value is required.");
            if (string.IsNullOrWhiteSpace(email))
                return Result<Appointment>.Fail("Email is required.");
            if (string.IsNullOrWhiteSpace(customerName))
                return Result<Appointment>.Fail("Customer name is required.");
            if (string.IsNullOrWhiteSpace(phone))
                return Result<Appointment>.Fail("Phone is required.");
            var phoneValidation = ValidateAndNormalizePhone(phone);
            if (!phoneValidation.Ok)
                return Result<Appointment>.Fail(phoneValidation.Error);
            if (string.IsNullOrWhiteSpace(contactType))
                return Result<Appointment>.Fail("Contact type is required.");

            var queue = _queues.Get(queueId);
            if (queue == null) return Result<Appointment>.Fail("Queue not found.");
            var referenceValidation = ValidateReference(refCriteria, referenceValue, streetNumber, streetName, streetType);
            if (!referenceValidation.Ok)
                return Result<Appointment>.Fail(referenceValidation.Error);
            if (!long.TryParse(serviceId, out var parsedServiceId) || parsedServiceId <= 0)
                return Result<Appointment>.Fail("Service is required.");
            var queueValidation = ValidateScheduledInputAgainstQueueDetails(queueId, parsedServiceId, contactType, refCriteria, scheduledFor);
            if (!queueValidation.Ok)
                return Result<Appointment>.Fail(queueValidation.Error);
            var slotValidation = ValidateScheduledTimeSlot(queueId, scheduledFor);
            if (!slotValidation.Ok)
                return Result<Appointment>.Fail(slotValidation.Error);

            var now = _clock.UtcNow;
            var user = string.IsNullOrWhiteSpace(stampUser) ? "web" : stampUser.Trim();
            var customer = GetOrCreateCustomer(customerName, email, phone, !string.IsNullOrWhiteSpace(meetingUrl), user, now);
            var customerTimeValidation = ValidateCustomerTimeAvailability(customer.Id, scheduledFor);
            if (!customerTimeValidation.Ok)
                return Result<Appointment>.Fail(customerTimeValidation.Error);

            var appt = new Appointment
            {
                Id = 0,
                EntityId = queue.EntityId,
                QueueId = queueId,
                CustomerId = customer.Id,
                CustomerEmail = customer.Email,
                CustomerFirstName = customer.FirstName ?? string.Empty,
                CustomerLastName = customer.LastName ?? string.Empty,
                CustomerPhone = customer.Phone ?? string.Empty,
                CustomerSmsOptIn = customer.SmsOptIn,
                ServiceId = parsedServiceId,
                RefCriteria = refCriteria,
                RefValue = referenceValue,
                ContactType = contactType.Trim(),
                MoreInfo = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim(),
                LanguagePreference = NormalizeLanguagePreference(languagePreference),
                Status = AppointmentStatus.Scheduled,
                CreatedBy = user,
                StampUser = user,
                CreatedOnUtc = now,
                StampDateUtc = now,
                CreatedUtc = now,
                UpdatedUtc = now
            };
            appt.ScheduledFor = scheduledFor;

            _appts.Add(appt);
            var insertedAppt = _appts.Get(appt.Id) ?? appt;
            if (string.IsNullOrWhiteSpace(insertedAppt.CustomerEmail))
            {
                insertedAppt.CustomerEmail = appt.CustomerEmail;
            }
            if (string.IsNullOrWhiteSpace(insertedAppt.CustomerFirstName))
            {
                insertedAppt.CustomerFirstName = appt.CustomerFirstName;
            }
            if (string.IsNullOrWhiteSpace(insertedAppt.CustomerLastName))
            {
                insertedAppt.CustomerLastName = appt.CustomerLastName;
            }
            if (string.IsNullOrWhiteSpace(insertedAppt.CustomerPhone))
            {
                insertedAppt.CustomerPhone = appt.CustomerPhone;
            }
            var emailWarning = SendAppointmentConfirmation(insertedAppt, queue, customerName, parsedServiceId);
            _rt.AppointmentChanged(insertedAppt);
            _rt.QueueChanged(insertedAppt.EntityId, insertedAppt.QueueId);

            return Result<Appointment>.Success(insertedAppt, emailWarning);
        }

        public Result<long> CreateWalkin(
            long queueId,
            string serviceId,
            string refValue,
            string permitNumber,
            string streetNumber,
            string streetName,
            string streetType,
            string email,
            string customerName,
            string phone,
            string contactType,
            string languagePreference,
            string meetingUrl,
            string notes,
            string stampUser)
        {
            var refCriteria = string.IsNullOrWhiteSpace(refValue) ? null : refValue.Trim();
            var referenceValue = BuildReferenceValue(refCriteria, permitNumber, streetNumber, streetName, streetType);

            if (queueId <= 0)
                return Result<long>.Fail("Queue is required.");
            if (string.IsNullOrWhiteSpace(referenceValue))
                return Result<long>.Fail("Reference value is required.");
            if (string.IsNullOrWhiteSpace(email))
                return Result<long>.Fail("Email is required.");
            if (string.IsNullOrWhiteSpace(customerName))
                return Result<long>.Fail("Customer name is required.");
            if (string.IsNullOrWhiteSpace(phone))
                return Result<long>.Fail("Phone is required.");
            var phoneValidation = ValidateAndNormalizePhone(phone);
            if (!phoneValidation.Ok)
                return Result<long>.Fail(phoneValidation.Error);
            if (string.IsNullOrWhiteSpace(contactType))
                return Result<long>.Fail("Contact type is required.");

            var queue = _queues.Get(queueId);
            if (queue == null) return Result<long>.Fail("Queue not found.");
            var referenceValidation = ValidateReference(refCriteria, referenceValue, streetNumber, streetName, streetType);
            if (!referenceValidation.Ok)
                return Result<long>.Fail(referenceValidation.Error);

            var now = _clock.UtcNow;
            var localNow = DateTime.Now;
            var user = string.IsNullOrWhiteSpace(stampUser) ? "web" : stampUser.Trim();
            var customer = GetOrCreateCustomer(customerName, email, phone, false, user, now);

            var walkin = new Appointment
            {
                Id = 0,
                EntityId = queue.EntityId,
                QueueId = queueId,
                CustomerId = customer.Id,
                ServiceId = long.TryParse(serviceId, out var parsedServiceId) ? parsedServiceId : (long?)null,
                RefCriteria = refCriteria,
                RefValue = referenceValue,
                ContactType = contactType.Trim(),
                MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim(),
                MoreInfo = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                LanguagePreference = NormalizeLanguagePreference(languagePreference),
                Status = AppointmentStatus.Arrived,
                CreatedBy = user,
                StampUser = user,
                CreatedOnUtc = now,
                StampDateUtc = now,
                CreatedUtc = now,
                UpdatedUtc = now
            };
            walkin.ScheduledFor = localNow;

            var newId = _appts.AddWalkin(walkin);
            _rt.AppointmentChanged(walkin);
            _rt.QueueChanged(walkin.EntityId, walkin.QueueId);

            return Result<long>.Success(newId);
        }

        private static string NormalizeLanguagePreference(string value)
        {
            var normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized == "EN" || normalized == "ES" || normalized == "CP")
            {
                return normalized;
            }

            return null;
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

            var queue = _queues.Get(appt.QueueId);
            var emailWarning = SendAppointmentCancellationEmail(appt, queue);

            _rt.AppointmentChanged(appt);
            _rt.QueueChanged(appt.EntityId, appt.QueueId);

            return string.IsNullOrWhiteSpace(emailWarning)
                ? Result.Success()
                : Result.SuccessWithWarning(emailWarning);
        }

        public AppointmentSnapshotDto GetAppointmentSnapshot(long appointmentId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return null;

            //var location = _locations.Get(appt.EntityId);
            var queue = _queues.Get(appt.QueueId);

            var snapshot = new AppointmentSnapshotDto
            {
                AppointmentId = appt.Id,
                EntityId = appt.EntityId,
                QueueId = appt.QueueId,
                //EntityName = location?.Name ?? "Unknown",
                QueueName = queue?.Name ?? "Unknown",
                Status = appt.Status.ToString(),
                ScheduledFor = appt.ScheduledFor.ToString("yyyy-MM-dd h:mm tt"),
                UpdatedUtc = (appt.UpdatedUtc.Kind == DateTimeKind.Utc ? appt.UpdatedUtc.ToLocalTime() : appt.UpdatedUtc).ToString("yyyy-MM-dd h:mm tt"),
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

        public Customer GetCustomerByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            return _customers.GetByEmail(email.Trim());
        }

        public IList<QueueOpenSlot> GetQueueOpenSlots(long queueId, DateTime dateLocal)
        {
            return _appts.GetQueueOpenSlots(queueId, dateLocal.Date);
        }

        public Result ValidateCustomerTimeSelection(string email, string phone, DateTime scheduledFor)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            var normalizedPhone = NormalizePhone(phone);
            Customer customer = null;
            if (!string.IsNullOrWhiteSpace(normalizedEmail))
                customer = _customers.GetByEmail(normalizedEmail);
            if (customer == null && !string.IsNullOrWhiteSpace(normalizedPhone))
                customer = _customers.GetByPhone(normalizedPhone);
            if (customer == null)
                return Result.Success();

            return ValidateCustomerTimeAvailability(customer.Id, scheduledFor);
        }

        private Customer GetOrCreateCustomer(string name, string email, string phone, bool smsOptIn, string stampUser, DateTime now)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            var normalizedPhone = NormalizePhone(phone);
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                throw new InvalidOperationException("Email is required.");
            }
            var customer = !string.IsNullOrWhiteSpace(normalizedEmail)
                ? _customers.GetByEmail(normalizedEmail)
                : null;
            if (customer == null)
            {
                customer = _customers.GetByPhone(normalizedPhone);
            }

            if (customer == null)
            {
                customer = new Customer
                {
                    Id = 0,
                    Phone = normalizedPhone,
                    Name = (name ?? string.Empty).Trim(),
                    SmsOptIn = smsOptIn,
                    ActiveFlag = true,
                    CreatedUtc = now,
                    UpdatedUtc = now,
                    StampDateUtc = now,
                    StampUser = stampUser
                };
                customer.Email = normalizedEmail;
                _customers.Add(customer);
                return customer;
            }

            customer.SmsOptIn = smsOptIn;
            if (!string.IsNullOrWhiteSpace(name))
            {
                customer.Name = name.Trim();
            }
            customer.Email = normalizedEmail;
            customer.Phone = normalizedPhone;
            customer.UpdatedUtc = now;
            customer.StampDateUtc = now;
            customer.StampUser = stampUser;
            _customers.Update(customer);
            return customer;
        }

        private static Result<string> ValidateAndNormalizePhone(string phone)
        {
            var normalizedPhone = NormalizePhone(phone);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                return Result<string>.Fail("Enter a valid US phone number.");
            }

            return Result<string>.Success(normalizedPhone);
        }

        private static string NormalizePhone(string phone)
        {
            var digits = Regex.Replace(phone ?? string.Empty, "[^0-9]", string.Empty);
            if (digits.Length == 11 && digits.StartsWith("1", StringComparison.Ordinal))
            {
                digits = digits.Substring(1);
            }

            if (digits.Length != 10)
            {
                return string.Empty;
            }

            if (digits[0] == '0' || digits[0] == '1' || digits[3] == '0' || digits[3] == '1')
            {
                return string.Empty;
            }

            return string.Format(CultureInfo.InvariantCulture, "({0}) {1}-{2}",
                digits.Substring(0, 3),
                digits.Substring(3, 3),
                digits.Substring(6, 4));
        }

        public Result ValidatePermit(string permitNumber)
        {
            return ValidatePermitNumber(permitNumber);
        }

        public Result ValidateReference(string referenceType, string enterValue, string streetNumber, string streetName, string streetType)
        {
            var normalizedType = (referenceType ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedType))
            {
                return Result.Fail("Reference type is required.");
            }

            if (string.Equals(normalizedType, "P", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedType, "Permit", StringComparison.OrdinalIgnoreCase))
            {
                return ValidatePermitNumber(enterValue);
            }

            if (string.Equals(normalizedType, "C", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedType, "Case", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateCaseNumber(enterValue);
            }

            if (string.Equals(normalizedType, "A", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedType, "Address", StringComparison.OrdinalIgnoreCase))
            {
                return ValidateAddress(streetNumber, streetName, streetType);
            }

            return Result.Success();
        }

        private static Result ValidatePermitNumber(string permitNumber)
        {
            return ValidateDirectReferenceValue(permitNumber, "Permit number is required.", "Permit number is invalid.");
        }

        private static Result ValidateCaseNumber(string caseNumber)
        {
            return ValidateDirectReferenceValue(caseNumber, "Case number is required.", "Case number is invalid.");
        }

        private static Result ValidateDirectReferenceValue(string value, string requiredMessage, string invalidMessage)
        {
            var apiBaseUrl = ConfigurationManager.AppSettings["FTAPIV1BaseUrl"];
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                return Result.Fail("Permit validation service is not configured.");
            }

            var apiKey = ConfigurationManager.AppSettings["FTApiKeyPolymorphic"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Result.Fail("Permit validation API key is not configured.");
            }

            var normalizedValue = (value ?? string.Empty).Trim();
            if (normalizedValue.Length == 0)
            {
                return Result.Fail(requiredMessage);
            }

            var requestUrl = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}/{1}",
                apiBaseUrl.TrimEnd('/'),
                Uri.EscapeDataString(normalizedValue));

            return ExecuteReferenceValidation(requestUrl, apiKey, invalidMessage);
        }

        private static Result ValidateAddress(string streetNumber, string streetName, string streetType)
        {
            var apiBaseUrl = ConfigurationManager.AppSettings["FTAPIV1BaseUrl"];
            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                return Result.Fail("Reference validation service is not configured.");
            }

            var apiKey = ConfigurationManager.AppSettings["FTApiKeyPolymorphic"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Result.Fail("Reference validation API key is not configured.");
            }

            var normalizedStreetNumber = (streetNumber ?? string.Empty).Trim();
            var normalizedStreetName = (streetName ?? string.Empty).Trim();
            var normalizedStreetType = (streetType ?? string.Empty).Trim();

            if (normalizedStreetNumber.Length == 0)
            {
                return Result.Fail("Street number is required.");
            }
            if (normalizedStreetName.Length == 0)
            {
                return Result.Fail("Street name is required.");
            }

            var query = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "StreetNumber={0}&StreetName={1}",
                Uri.EscapeDataString(normalizedStreetNumber),
                Uri.EscapeDataString(normalizedStreetName));
            if (!string.IsNullOrWhiteSpace(normalizedStreetType))
            {
                query += "&StreetType=" + Uri.EscapeDataString(normalizedStreetType);
            }

            var requestUrl = apiBaseUrl.TrimEnd('/') + "/search?" + query;
            return ExecuteReferenceValidation(requestUrl, apiKey, "Address is invalid.");
        }

        private static Result ExecuteReferenceValidation(string requestUrl, string apiKey, string invalidMessage)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    httpClient.DefaultRequestHeaders.Add("FTApiKeyPolymorphic", apiKey);

                    var response = httpClient.GetAsync(requestUrl).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        return Result.Success();
                    }

                    return Result.Fail(invalidMessage);
                }
            }
            catch (TaskCanceledException)
            {
                return Result.Fail("Validation request timed out.");
            }
            catch (HttpRequestException)
            {
                return Result.Fail("Error connecting to validation service.");
            }
            catch (Exception)
            {
                return Result.Fail("An error occurred during validation.");
            }
        }

        private Result ValidateScheduledInputAgainstQueueDetails(long queueId, long serviceId, string contactType, string refValue, DateTime scheduledFor)
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
                if (string.IsNullOrWhiteSpace(contactCode))
                    return Result.Fail("Contact type is required.");

                var contactCodes = ReadOptionCodes(detailsJson?["contactoptions"], "type_key");
                if (contactCodes.Count > 0 && !contactCodes.Contains(contactCode))
                    return Result.Fail("Selected contact type is not valid for this queue.");

                var refCode = (refValue ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(refCode))
                {
                    var refCodes = ReadOptionCodes(detailsJson?["refoptions"], "ref_key");
                    if (refCodes.Count > 0 && !refCodes.Contains(refCode))
                        return Result.Fail("Selected reference is not valid for this queue.");
                }

                if (!IsScheduledSlotAllowed(schedulesJson?["schedules"], scheduledFor))
                    return Result.Fail("Selected date/time is not valid for this queue schedule.");
            }
            catch
            {
                return Result.Fail("Could not validate queue details.");
            }

            return Result.Success();
        }

        private Result ValidateScheduledTimeSlot(long queueId, DateTime scheduledFor)
        {
            var localScheduled = scheduledFor;
            var slots = _appts.GetQueueOpenSlots(queueId, localScheduled.Date);
            if (slots == null || slots.Count == 0)
                return Result.Fail("No available time slots were found for the selected date.");

            var matched = slots.Any(slot => SlotMatches(slot, localScheduled));
            return matched
                ? Result.Success()
                : Result.Fail("Selected time is no longer available for this queue.");
        }

        private Result ValidateCustomerTimeAvailability(long customerId, DateTime scheduledFor)
        {
            if (customerId <= 0)
                return Result.Success();

            var conflict = _appts.ListByCustomer(customerId)
                .Any(a =>
                    a.Id > 0 &&
                    a.ScheduledFor == scheduledFor &&
                    a.Status != AppointmentStatus.Cancelled &&
                    a.Status != AppointmentStatus.ClosedBySystem &&
                    a.Status != AppointmentStatus.Completed &&
                    a.Status != AppointmentStatus.TransferredOut);

            return conflict
                ? Result.Fail("Customer already has an appointment scheduled for this date and time.")
                : Result.Success();
        }

        private static bool SlotMatches(QueueOpenSlot slot, DateTime localScheduled)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.SlotBegin))
                return false;

            if (!DateTime.TryParseExact(slot.SlotBegin.Trim(), new[] { "h:mm tt", "hh:mm tt" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedBegin))
                return false;

            return parsedBegin.TimeOfDay == localScheduled.TimeOfDay;
        }

        private static JObject ParseJsonObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            return JObject.Parse(json);
        }

        private static System.Collections.Generic.HashSet<string> ReadOptionCodes(JToken listToken, string codeField)
        {
            var set = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

        private static bool IsScheduledSlotAllowed(JToken schedulesToken, DateTime scheduledFor)
        {
            var schedules = schedulesToken as JArray;
            if (schedules == null || schedules.Count == 0)
                return false;

            var local = scheduledFor;
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

        private static string BuildReferenceValue(string refCriteria, string enteredValue, string streetNumber, string streetName, string streetType)
        {
            if (string.Equals((refCriteria ?? string.Empty).Trim(), "A", StringComparison.OrdinalIgnoreCase))
            {
                var parts = new[]
                {
                    (streetNumber ?? string.Empty).Trim(),
                    (streetName ?? string.Empty).Trim(),
                    (streetType ?? string.Empty).Trim()
                }.Where(v => !string.IsNullOrWhiteSpace(v));
                return string.Join(" ", parts);
            }

            return string.IsNullOrWhiteSpace(enteredValue) ? null : enteredValue.Trim();
        }

        private string SendAppointmentConfirmation(Appointment appointment, Queue queue, string customerName, long serviceId)
        {
            if (appointment == null)
                return null;

            var loginUrl = ConfigurationManager.AppSettings["AppointmentLoginUrl"] ?? "#";
            var inPersonLocation = ConfigurationManager.AppSettings["AppointmentInPersonLocation"] ?? "TBD";
            var queueName = queue?.Name ?? "Queue";
            var serviceName = _queues.ListServicesByQueue(queue?.Id ?? 0)
                .FirstOrDefault(s => s.Item1 == serviceId)?.Item2 ?? queueName;

            var emailWarning = SendAppointmentConfirmationEmail(appointment, customerName, queueName, serviceName, inPersonLocation, loginUrl);
            SendAppointmentConfirmationSms(appointment, customerName, queueName, serviceName, inPersonLocation, loginUrl);
            return emailWarning;
        }

        private string SendAppointmentConfirmationEmail(Appointment appointment, string customerName, string queueName, string serviceName, string inPersonLocation, string loginUrl)
        {
            try
            {
                var host = ConfigurationManager.AppSettings["AppointmentMailHost"];
                var fromEmail = ConfigurationManager.AppSettings["AppointmentMailFrom"];
                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
                    return "Confirmation email was not sent: mail host/from configuration is missing.";

                var toEmail = (appointment.CustomerEmail ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(toEmail))
                    return "Confirmation email was not sent: customer email is missing.";

                var portValue = ConfigurationManager.AppSettings["AppointmentMailPort"];
                if (!int.TryParse(portValue, out var port) || port <= 0)
                    port = 25;

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail);
                    message.To.Add(toEmail);
                    message.Subject = "Appointment Confirmation";
                    message.Body = BuildAppointmentConfirmationHtml(appointment, customerName, queueName, serviceName, inPersonLocation, loginUrl);
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(host, port))
                    {
                        var enableSslValue = ConfigurationManager.AppSettings["AppointmentMailEnableSsl"];
                        if (bool.TryParse(enableSslValue, out var enableSsl))
                            client.EnableSsl = enableSsl;

                        var username = ConfigurationManager.AppSettings["AppointmentMailUsername"];
                        var password = ConfigurationManager.AppSettings["AppointmentMailPassword"];
                        if (!string.IsNullOrWhiteSpace(username))
                            client.Credentials = new System.Net.NetworkCredential(username, password ?? string.Empty);

                        client.Send(message);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LogNotificationError("email", appointment?.Id ?? 0, ex.ToString());
                return "Confirmation email failed: " + ex.Message;
            }
        }

        private void SendAppointmentConfirmationSms(Appointment appointment, string customerName, string queueName, string serviceName, string inPersonLocation, string loginUrl)
        {
            try
            {
                var phone = (appointment.CustomerPhone ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(phone) || !appointment.CustomerSmsOptIn)
                    return;

                var message = BuildAppointmentConfirmationSms(appointment, customerName, queueName, serviceName, inPersonLocation, loginUrl);
                if (string.IsNullOrWhiteSpace(message))
                    return;

                var result = SendSmsViaProc(phone, message, string.IsNullOrWhiteSpace(appointment.StampUser) ? "web" : appointment.StampUser.Trim());
                if (!string.IsNullOrWhiteSpace(result))
                {
                    Trace.TraceInformation("SEND_SMS result for appointment {0}: {1}", appointment.Id, result);
                    Console.Error.WriteLine("SEND_SMS result for appointment {0}: {1}", appointment.Id, result);
                }
            }
            catch (Exception ex)
            {
                LogNotificationError("sms", appointment?.Id ?? 0, ex.ToString());
            }
        }

        private string SendAppointmentCancellationEmail(Appointment appointment, Queue queue)
        {
            try
            {
                var host = ConfigurationManager.AppSettings["AppointmentMailHost"];
                var fromEmail = ConfigurationManager.AppSettings["AppointmentMailFrom"];
                if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail) || appointment == null)
                    return "Cancellation email was not sent: mail host/from configuration is missing.";

                var toEmail = (appointment.CustomerEmail ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(toEmail))
                    return "Cancellation email was not sent: customer email is missing.";

                var portValue = ConfigurationManager.AppSettings["AppointmentMailPort"];
                if (!int.TryParse(portValue, out var port) || port <= 0)
                    port = 25;

                var customerName = string.Join(" ", new[]
                {
                    (appointment.CustomerFirstName ?? string.Empty).Trim(),
                    (appointment.CustomerLastName ?? string.Empty).Trim()
                }.Where(v => !string.IsNullOrWhiteSpace(v))).Trim();
                var queueName = queue?.Name ?? "Queue";
                var serviceName = _queues.ListServicesByQueue(queue?.Id ?? 0)
                    .FirstOrDefault(s => s.Item1 == (appointment.ServiceId ?? 0))?.Item2 ?? queueName;

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail);
                    message.To.Add(toEmail);
                    message.Subject = "Appointment Cancellation";
                    message.Body = BuildAppointmentCancellationHtml(appointment, customerName, queueName, serviceName);
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(host, port))
                    {
                        var enableSslValue = ConfigurationManager.AppSettings["AppointmentMailEnableSsl"];
                        if (bool.TryParse(enableSslValue, out var enableSsl))
                            client.EnableSsl = enableSsl;

                        var username = ConfigurationManager.AppSettings["AppointmentMailUsername"];
                        var password = ConfigurationManager.AppSettings["AppointmentMailPassword"];
                        if (!string.IsNullOrWhiteSpace(username))
                            client.Credentials = new System.Net.NetworkCredential(username, password ?? string.Empty);

                        client.Send(message);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LogNotificationError("cancellation email", appointment?.Id ?? 0, ex.ToString());
                return "Cancellation email failed: " + ex.Message;
            }
        }

        private static void LogNotificationError(string channel, long appointmentId, string message)
        {
            var output = string.Format(
                CultureInfo.InvariantCulture,
                "Failed to send appointment {0} for appointment {1}: {2}",
                channel ?? "notification",
                appointmentId,
                message ?? string.Empty);
            Trace.TraceError(output);
            Console.Error.WriteLine(output);
        }

        private static string BuildAppointmentConfirmationHtml(Appointment appointment, string customerName, string queueName, string serviceName, string inPersonLocation, string loginUrl)
        {
            var safeCustomerName = HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(customerName) ? "Customer" : customerName.Trim());
            var safeQueueName = HttpUtility.HtmlEncode(queueName ?? string.Empty);
            var safeServiceName = HttpUtility.HtmlEncode(serviceName ?? string.Empty);
            var safeAppointmentType = HttpUtility.HtmlEncode(GetContactMethodText(appointment.ContactType));
            var safeLocation = HttpUtility.HtmlEncode(inPersonLocation ?? "TBD");
            var safePhone = HttpUtility.HtmlEncode(appointment.CustomerPhone ?? string.Empty);
            var safeLoginUrl = HttpUtility.HtmlAttributeEncode(loginUrl ?? "#");
            var appointmentTime = HttpUtility.HtmlEncode(appointment.ScheduledFor.ToString("MMMM dd, yyyy h:mm tt"));
            var displayLink = BuildMeetingLinkHtml(appointment.MeetingUrl);

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("    <meta charset=\"utf-8\" />");
            html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
            html.AppendLine("    <title>Appointment Confirmation</title>");
            html.AppendLine("    <style>");
            html.AppendLine("        body { font-family: Arial, sans-serif; font-size: 14px; line-height: 1.6; color: #333; margin: 0; padding: 20px; }");
            html.AppendLine("        .container { max-width: 600px; margin: 0 auto; }");
            html.AppendLine("        h2 { color: #667eea; font-size: 18px; margin-bottom: 16px; }");
            html.AppendLine("        p { margin: 0 0 12px 0; }");
            html.AppendLine("        .details { margin: 16px 0; padding: 12px; background: #f5f5f5; border-radius: 4px; }");
            html.AppendLine("        .details p { margin: 6px 0; }");
            html.AppendLine("        .footer { margin-top: 24px; font-size: 12px; color: #666; }");
            html.AppendLine("        a { color: #007bff; }");
            html.AppendLine("    </style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("    <div class=\"container\">");
            html.AppendLine($"        <p>Dear {safeCustomerName},</p>");
            html.AppendLine($"        <p>Your appointment with Orange County {safeQueueName} is confirmed. See below for details:</p>");
            html.AppendLine("        <div class=\"details\">");
            html.AppendLine($"            <p><strong>Appointment Time:</strong> {appointmentTime}</p>");
            html.AppendLine($"            <p><strong>Appointment Type:</strong> {safeAppointmentType}</p>");
            html.AppendLine($"            <p><strong>Queue:</strong> {safeQueueName}</p>");
            html.AppendLine($"            <p><strong>Service:</strong> {safeServiceName}</p>");
            html.AppendLine("        </div>");
            html.AppendLine($"        <p><strong>For In-person:</strong> {safeLocation}</p>");
            html.AppendLine($"        <p><strong>For Online:</strong> A virtual appointment request has been submitted and will be conducted through Webex at {displayLink}. Prior to the meeting, please follow the instructions below:</p>");
            html.AppendLine("        <p>Webex Instructions, English | Spanish | Creole</p>");
            html.AppendLine($"        <p><strong>For Phone:</strong> Our staff will contact you at the phone number provided ({safePhone}) at the scheduled time.</p>");
            html.AppendLine("        <p>Sincerely,</p>");
            html.AppendLine("        <p>Orange County Government, FL</p>");
            html.AppendLine("        <div class=\"footer\">");
            html.AppendLine($"            <p>You are responsible to <a href=\"{safeLoginUrl}\">Log In</a> to the Appointment System to review your Upcoming Appointments.</p>");
            html.AppendLine("            <p>Orange County reserves the right to modify or reschedule your appointment date and time, based on the availability of staff and other considerations.</p>");
            html.AppendLine("            <p>If you cannot attend this appointment, as a courtesy, please cancel this appointment from your dashboard as soon as possible.</p>");
            html.AppendLine("        </div>");
            html.AppendLine("    </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");
            return html.ToString();
        }

        private static string BuildAppointmentConfirmationSms(Appointment appointment, string customerName, string queueName, string serviceName, string inPersonLocation, string loginUrl)
        {
            var appointmentTime = appointment.ScheduledFor.ToString("MMMM dd, yyyy h:mm tt", CultureInfo.InvariantCulture);
            var appointmentType = GetContactMethodText(appointment.ContactType);
            var cleanQueueName = (queueName ?? string.Empty).Trim();
            var cleanServiceName = (serviceName ?? string.Empty).Trim();
            var cleanLocation = (inPersonLocation ?? "TBD").Trim();
            var cleanPhone = (appointment.CustomerPhone ?? string.Empty).Trim();
            var cleanMeetingUrl = (appointment.MeetingUrl ?? string.Empty).Trim();
            var cleanLoginUrl = (loginUrl ?? string.Empty).Trim();

            var text = new StringBuilder();
            text.Append("Appointment Confirmation: ");
            if (!string.IsNullOrWhiteSpace(cleanQueueName))
            {
                text.Append("Orange County ");
                text.Append(cleanQueueName);
                text.Append(". ");
            }

            text.Append("Time: ");
            text.Append(appointmentTime);
            text.Append(". Type: ");
            text.Append(appointmentType);
            text.Append(". ");

            if (!string.IsNullOrWhiteSpace(cleanServiceName))
            {
                text.Append("Service: ");
                text.Append(cleanServiceName);
                text.Append(". ");
            }

            if (string.Equals((appointment.ContactType ?? string.Empty).Trim(), "OM", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(cleanMeetingUrl))
            {
                text.Append("Meeting link: ");
                text.Append(cleanMeetingUrl);
                text.Append(". ");
            }
            else if (string.Equals((appointment.ContactType ?? string.Empty).Trim(), "IP", StringComparison.OrdinalIgnoreCase))
            {
                text.Append("Location: ");
                text.Append(cleanLocation);
                text.Append(". ");
            }
            else if (string.Equals((appointment.ContactType ?? string.Empty).Trim(), "PC", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(cleanPhone))
            {
                text.Append("We will call ");
                text.Append(cleanPhone);
                text.Append(". ");
            }

            if (!string.IsNullOrWhiteSpace(cleanLoginUrl) && cleanLoginUrl != "#")
            {
                text.Append("Login: ");
                text.Append(cleanLoginUrl);
                text.Append(". ");
            }

            text.Append("Reply STOP to stop");
            return text.ToString();
        }

        private static string BuildAppointmentCancellationHtml(Appointment appointment, string customerName, string queueName, string serviceName)
        {
            var safeCustomerName = HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(customerName) ? "Customer" : customerName.Trim());
            var safeQueueName = HttpUtility.HtmlEncode(queueName ?? string.Empty);
            var safeServiceName = HttpUtility.HtmlEncode(serviceName ?? string.Empty);
            var safeAppointmentType = HttpUtility.HtmlEncode(GetContactMethodText(appointment.ContactType));
            var appointmentTime = HttpUtility.HtmlEncode(appointment.ScheduledFor.ToString("MMMM dd, yyyy h:mm tt"));

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" /><title>Appointment Cancellation</title></head>");
            html.AppendLine("<body style=\"font-family:Arial,sans-serif;font-size:14px;line-height:1.6;color:#333;margin:0;padding:20px;\">");
            html.AppendLine("<div style=\"max-width:600px;margin:0 auto;\">");
            html.AppendLine($"<p>Dear {safeCustomerName},</p>");
            html.AppendLine("<p>Your appointment has been cancelled.</p>");
            html.AppendLine("<div style=\"margin:16px 0;padding:12px;background:#f5f5f5;border-radius:4px;\">");
            html.AppendLine($"<p><strong>Appointment Time:</strong> {appointmentTime}</p>");
            html.AppendLine($"<p><strong>Appointment Type:</strong> {safeAppointmentType}</p>");
            html.AppendLine($"<p><strong>Queue:</strong> {safeQueueName}</p>");
            html.AppendLine($"<p><strong>Service:</strong> {safeServiceName}</p>");
            html.AppendLine("</div>");
            html.AppendLine("<p>If you still need assistance, please create a new appointment.</p>");
            html.AppendLine("<p>Sincerely,</p>");
            html.AppendLine("<p>Orange County Government, FL</p>");
            html.AppendLine("</div></body></html>");
            return html.ToString();
        }

        private static string SendSmsViaProc(string phone, string msg, string user)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["FastQOracle"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
                return "FastQOracle connection string is missing.";

            using (var conn = new OracleConnection(connectionString))
            using (var cmd = new OracleCommand("SEND_SMS", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new OracleParameter("p_PHONE_NUMBER", OracleDbType.Varchar2, phone ?? string.Empty, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_BODY", OracleDbType.Varchar2, msg ?? string.Empty, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_MEDIAURL", OracleDbType.Varchar2, string.Empty, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_STAMPUSER", OracleDbType.Varchar2, string.IsNullOrWhiteSpace(user) ? "web" : user, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_APP_COL_NAME", OracleDbType.Varchar2, DBNull.Value, ParameterDirection.Input));
                cmd.Parameters.Add(new OracleParameter("p_APP_COL_VAL", OracleDbType.Varchar2, DBNull.Value, ParameterDirection.Input));

                var outRes = new OracleParameter("p_out_res", OracleDbType.Varchar2, 4000)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outRes);

                conn.Open();
                cmd.ExecuteNonQuery();

                return outRes.Value == DBNull.Value ? string.Empty : outRes.Value?.ToString() ?? string.Empty;
            }
        }

        private static string BuildMeetingLinkHtml(string meetingUrl)
        {
            var safeUrl = (meetingUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(safeUrl))
                return "the provided meeting link";

            var encodedUrl = HttpUtility.HtmlAttributeEncode(safeUrl);
            var encodedText = HttpUtility.HtmlEncode(safeUrl);
            return $"<a href=\"{encodedUrl}\">{encodedText}</a>";
        }

        private static string GetContactMethodText(string contactType)
        {
            var normalized = (contactType ?? string.Empty).Trim().ToUpperInvariant();
            switch (normalized)
            {
                case "PC":
                    return "Phone";
                case "OM":
                    return "Online";
                case "IP":
                    return "In-Person";
                default:
                    return string.IsNullOrWhiteSpace(normalized) ? "In-Person" : normalized;
            }
        }

        private struct ScheduleWindow
        {
            public int Open;
            public int Close;
        }
    }
}
