using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class ProviderService
    {
        private readonly IAppointmentRepository _appts;
        private readonly ICustomerRepository _customers;
        private readonly IQueueRepository _queues;
        private readonly IServiceTransactionRepository _serviceTransactions;
        private readonly IRealtimeNotifier _rt;

        public ProviderService()
            : this(
                DbRepositoryFactory.CreateAppointmentRepository(),
                DbRepositoryFactory.CreateCustomerRepository(),
                DbRepositoryFactory.CreateQueueRepository(),
                DbRepositoryFactory.CreateServiceTransactionRepository(),
                new SignalRRealtimeNotifier())
        {
        }

        public ProviderService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            IServiceTransactionRepository serviceTransactions,
            IRealtimeNotifier rt)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            _serviceTransactions = serviceTransactions;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        public Result SaveServiceInfo(long appointmentId, char srcType, string guestUrl, string hostUrl, string notes, string stampUser)
        {
            var srcId = appointmentId;
            try
            {
                var user = string.IsNullOrWhiteSpace(stampUser) ? "web" : stampUser.Trim();
                _serviceTransactions.SaveServiceInfo(
                    srcType,
                    srcId,
                    string.IsNullOrWhiteSpace(guestUrl) ? null : guestUrl.Trim(),
                    string.IsNullOrWhiteSpace(hostUrl) ? null : hostUrl.Trim(),
                    string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
                    user);
                return Result.Success();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }

        public IList<Queue> ListQueues()
        {
            return ListEligibleQueues();
        }

        public IList<Queue> ListTransferQueues(long? entityId)
        {
            return _queues.ListByEntity(entityId);
        }

        public IList<Tuple<long, string>> ListTransferServices(long queueId)
        {
            return _queues.ListServicesByQueue(queueId);
        }

        public QueueDetailOptions GetQueueDetailOptions(long queueId)
        {
            if (queueId <= 0)
            {
                return null;
            }

            var jsonParts = _queues.GetQueueDetailsJson(queueId);
            if (jsonParts == null)
            {
                Trace.TraceWarning("GetQueueDetailOptions queueId={0}: GetQueueDetailsJson returned null tuple.", queueId);
                return null;
            }

            try
            {
                var servicesJson = ParseJsonObject(jsonParts.Item1);
                var schedulesJson = ParseJsonObject(jsonParts.Item2);
                var detailsJson = ParseJsonObject(jsonParts.Item3);
                var options = new QueueDetailOptions
                {
                    QueueId = queueId,
                    Services = ReadOptions(servicesJson?["services"], "service_id", "service_name"),
                    ContactOptions = ReadOptions(detailsJson?["contactoptions"], "type_key", "type_val"),
                    RefOptions = ReadOptions(detailsJson?["refoptions"], "ref_key", "ref_val"),
                    Schedules = ReadSchedules(schedulesJson?["schedules"])
                };

                Trace.TraceInformation(
                    "GetQueueDetailOptions queueId={0}: services={1}, contacts={2}, refs={3}, schedules={4}. RawJsonLength services={5}, schedules={6}, details={7}.",
                    queueId,
                    options.Services.Count,
                    options.ContactOptions.Count,
                    options.RefOptions.Count,
                    options.Schedules.Count,
                    SafeLength(jsonParts.Item1),
                    SafeLength(jsonParts.Item2),
                    SafeLength(jsonParts.Item3));

                if (options.Schedules.Count == 0)
                {
                    Trace.TraceWarning("GetQueueDetailOptions queueId={0}: schedules parsed as empty/null from VW_QUEUE_DETAILS_JSON.", queueId);
                }
                else
                {
                    foreach (var schedule in options.Schedules)
                    {
                        Trace.TraceInformation(
                            "GetQueueDetailOptions queueId={0}: scheduleId={1}, dateBegin='{2}', dateEnd='{3}', weeklySch='{4}', open='{5}', close='{6}', interval='{7}', resources={8}.",
                            queueId,
                            schedule.ScheduleId,
                            schedule.DateBegin ?? string.Empty,
                            schedule.DateEnd ?? string.Empty,
                            schedule.WeeklySchedule ?? string.Empty,
                            schedule.OpenTime ?? string.Empty,
                            schedule.CloseTime ?? string.Empty,
                            schedule.IntervalTime ?? string.Empty,
                            schedule.AvailableResources);
                    }
                }

                return options;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetQueueDetailOptions queueId={0} failed: {1}", queueId, ex);
                return null;
            }
        }

        private static int SafeLength(string value)
        {
            return string.IsNullOrEmpty(value) ? 0 : value.Length;
        }

        public IList<Customer> ListCustomers()
        {
            return _customers.ListAll();
        }

        //public IList<Appointment> ListAppointmentsForDate(DateTime date)
        //{
        //    var selectedDate = date.Date;
        //    return _appts.ListAll()
        //        .Where(a => a.ScheduledFor.Date == selectedDate)
        //        .ToList();
        //}

        public IList<ProviderAppointmentRow> BuildRows(
            IList<Appointment> appointments,
            IDictionary<long, Queue> queueMap,
            IDictionary<long, Customer> customerMap)
        {
            return appointments.Select(a =>
            {
                queueMap.TryGetValue(a.QueueId, out var queue);
                customerMap.TryGetValue(a.CustomerId, out var customer);

                var contact = GetContactMethodText(a.ContactType);
                var localScheduled = a.ScheduledFor;

                return new ProviderAppointmentRow
                {
                    AppointmentId = a.Id,
                    QueueId = a.QueueId,
                    ServiceId = a.ServiceId ?? 0,
                    ScheduledFor = a.ScheduledFor,
                    StartTimeText = localScheduled.ToString("h:mm tt"),
                    StartDateText = localScheduled.ToString("MMM dd, yyyy"),
                    QueueName = queue?.Name ?? "Unknown Queue",
                    ServiceType = queue?.Name != null ? $"Questions: {queue.Name}" : "Questions: General",
                    CustomerName = customer?.Name ?? "Unknown",
                    CustomerEmail = customer?.Email ?? string.Empty,
                    Phone = customer?.Phone ?? "-",
                    Status = a.Status,
                    StatusText = GetStatusText(a.Status),
                    ContactMethod = contact,
                    ContactTypeCode = a.ContactType,
                    RefValue = a.RefValue,
                    LanguagePreference = GetLanguagePreferenceText(a.LanguagePreference),
                    MeetingUrl = a.MeetingUrl,          //NormalizeMeetingUrl(a.MeetingUrl),
                    MeetingUrlHost = a.MeetingUrlHost,  // NormalizeMeetingUrl(a.MeetingUrlHost),
                    SmsOptIn = a.CustomerSmsOptIn,
                    StampUser = a.StampUser
                };
            }).OrderBy(r => r.ScheduledFor).ToList();
        }

        public IList<ProviderAppointmentRow> BuildRowsForUser(long entityId, string userId, DateTime rangeStart, DateTime rangeEnd)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<ProviderAppointmentRow>();
            }

            var startDate = rangeStart.Date;
            var endDate = rangeEnd.Date;
            var rows = _appts.ListForUser(entityId, userId, startDate, endDate);

            return BuildProviderRows(rows);
        }

        public IList<ProviderAppointmentRow> BuildWalkinsForUser(long entityId, string userId, DateTime rangeStart, DateTime rangeEnd)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<ProviderAppointmentRow>();
            }

            var startDate = rangeStart.Date;
            var endDate = rangeEnd.Date;
            var rows = _appts.ListWalkinsForUser(entityId, userId, startDate, endDate);

            return BuildProviderRows(rows);
        }

        private static IList<ProviderAppointmentRow> BuildProviderRows(IList<ProviderAppointmentData> rows)
        {
            return rows.Select(r =>
            {
                var serviceType = !string.IsNullOrWhiteSpace(r.ServiceName)
                    ? r.ServiceName
                    : (!string.IsNullOrWhiteSpace(r.QueueName) ? $"Questions: {r.QueueName}" : "Questions: General");
                var localScheduled = r.ScheduledFor;

                return new ProviderAppointmentRow
                {
                    AppointmentId = r.AppointmentId,
                    QueueId = r.QueueId,
                    ServiceId = r.ServiceId,
                    ScheduledFor = r.ScheduledFor,
                    StartTimeText = localScheduled.ToString("h:mm tt"),
                    StartDateText = localScheduled.ToString("MMM dd, yyyy"),
                    QueueName = r.QueueName ?? "Unknown Queue",
                    ServiceType = serviceType,
                    CustomerName = string.IsNullOrWhiteSpace(r.CustomerName) ? "Unknown" : r.CustomerName,
                    CustomerEmail = string.IsNullOrWhiteSpace(r.CustomerEmail) ? string.Empty : r.CustomerEmail,
                    Phone = string.IsNullOrWhiteSpace(r.CustomerPhone) ? "-" : r.CustomerPhone,
                    Status = r.Status,
                    StatusText = GetStatusText(r.Status),
                    ContactMethod = GetContactMethodText(r.ContactType),
                    ContactTypeCode = r.ContactType,
                    RefValue = r.RefValue,
                    LanguagePreference = GetLanguagePreferenceText(r.LanguagePreference),
                    MeetingUrl = r.MeetingUrl,          //NormalizeMeetingUrl(r.MeetingUrl),
                    MeetingUrlHost = r.MeetingUrlHost,  // NormalizeMeetingUrl(r.MeetingUrlHost),
                    Notes = string.IsNullOrWhiteSpace(r.Notes) ? string.Empty : r.Notes.Trim(),
                    ServiceNotes = string.IsNullOrWhiteSpace(r.ServiceNotes) ? string.Empty : r.ServiceNotes.Trim(),
                    ServiceStartTimeText = FormatTransactionTime(r.ServiceStartTime),
                    ServiceEndTimeText = FormatTransactionTime(r.ServiceEndTime),
                    SmsOptIn = r.SmsOptIn,
                    StampUser = r.StampUser,
                    StampUserName = r.StampUserName
                };  
            }).OrderBy(r => r.ScheduledFor).ToList();
        }

        private static string GetLanguagePreferenceText(string value)
        {
            var code = (value ?? string.Empty).Trim().ToUpperInvariant();
            if (code == "EN") return "English";
            if (code == "ES") return "Spanish";
            if (code == "CP") return "Creole";
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string FormatTransactionTime(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("MMM dd, yyyy h:mm tt") : string.Empty;
        }

        private static string NormalizeMeetingUrl(string value)
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return string.Empty;
            }

            if (trimmed.StartsWith("//"))
            {
                return "https:" + trimmed;
            }

            if (trimmed.Contains("://"))
            {
                return trimmed;
            }

            return "https://" + trimmed.TrimStart('/');
        }

        private void PopulateCustomerNotificationFields(Appointment appointment)
        {
            if (appointment == null || appointment.CustomerId <= 0)
            {
                return;
            }

            var needsName = string.IsNullOrWhiteSpace(appointment.CustomerFirstName) && string.IsNullOrWhiteSpace(appointment.CustomerLastName);
            var needsPhone = string.IsNullOrWhiteSpace(appointment.CustomerPhone);
            if (!needsName && !needsPhone)
            {
                return;
            }

            var customer = _customers.Get(appointment.CustomerId);
            if (customer == null)
            {
                return;
            }

            if (needsName)
            {
                appointment.CustomerFirstName = customer.FirstName ?? string.Empty;
                appointment.CustomerLastName = customer.LastName ?? string.Empty;
            }

            if (needsPhone)
            {
                appointment.CustomerPhone = customer.Phone ?? string.Empty;
            }
        }

        public long? GetSourceQueueId(char srcType, long sourceId)
        {
            return _appts.GetQueueIdForSource(srcType, sourceId);
        }

        private static string GetStatusText(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Arrived => "ARRIVED",
                AppointmentStatus.InService => "IN PROGRESS",
                AppointmentStatus.Completed => "DONE",
                AppointmentStatus.Cancelled => "CANCELLED",
                _ => status.ToString().ToUpperInvariant()
            };
        }

        private static string GetContactMethodText(string contactType)
        {
            var normalized = (contactType ?? string.Empty).Trim().ToUpperInvariant();
            return normalized switch
            {
                "PC" => "Phone Call",
                "OM" => "Online Meeting",
                "IP" => "In-Person",
                _ => string.IsNullOrWhiteSpace(normalized) ? "In-Person" : normalized
            };
        }

        public QueueSnapshotDto GetQueueSnapshot(long entityId, long queueId)
        {
            //var location = _locations.Get(entityId);
            var queue = _queues.Get(queueId);

            var all = _appts.ListByQueue(queueId)
                .OrderBy(a => a.Status)
                .ThenBy(a => a.CreatedOn)
                .ToList();

            bool IsWaiting(Appointment a) => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Arrived;
            bool IsInService(Appointment a) => a.Status == AppointmentStatus.InService;
            bool IsDone(Appointment a) => a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.ClosedBySystem || a.Status == AppointmentStatus.TransferredOut;

            var waiting = all.Where(IsWaiting).OrderBy(a => a.CreatedOn).ToList();
            var inService = all.Where(IsInService).OrderBy(a => a.UpdatedOn).ToList();
            var done = all.Where(IsDone).OrderByDescending(a => a.UpdatedOn).Take(50).ToList();

            var dto = new QueueSnapshotDto
            {
                EntityId = entityId,
                QueueId = queueId,
                //EntityName = location?.Name ?? "Unknown",
                QueueName = queue?.Name ?? "Unknown",
                WaitingCount = waiting.Count,
                InServiceCount = inService.Count,
                CompletedCount = done.Count
            };

            foreach (var a in waiting)
            {
                var c = _customers.Get(a.CustomerId);
                dto.Waiting.Add(new AppointmentRowDto
                {
                    AppointmentId = a.Id,
                    CustomerId = a.CustomerId,
                    CustomerPhone = c?.Phone ?? "",
                    Status = a.Status.ToString(),
                    ScheduledFor = a.ScheduledFor.ToString("yyyy-MM-dd h:mm tt"),
                    UpdatedOn = a.UpdatedOn.ToString("yyyy-MM-dd h:mm tt")
                });
            }

            foreach (var a in inService)
            {
                var c = _customers.Get(a.CustomerId);
                dto.InService.Add(new AppointmentRowDto
                {
                    AppointmentId = a.Id,
                    CustomerId = a.CustomerId,
                    CustomerPhone = c?.Phone ?? "",
                    Status = a.Status.ToString(),
                    ScheduledFor = a.ScheduledFor.ToString("yyyy-MM-dd h:mm tt"),
                    UpdatedOn = a.UpdatedOn.ToString("yyyy-MM-dd h:mm tt")
                });
            }

            foreach (var a in done)
            {
                var c = _customers.Get(a.CustomerId);
                dto.Done.Add(new AppointmentRowDto
                {
                    AppointmentId = a.Id,
                    CustomerId = a.CustomerId,
                    CustomerPhone = c?.Phone ?? "",
                    Status = a.Status.ToString(),
                    ScheduledFor = a.ScheduledFor.ToString("yyyy-MM-dd h:mm tt"),
                    UpdatedOn = a.UpdatedOn.ToString("yyyy-MM-dd h:mm tt")
                });
            }

            return dto;
        }

        private IList<Queue> ListEligibleQueues(long? requestedEntityId = null)
        {
            var auth = new AuthService();
            var sessionEntityId = auth.GetSessionEntityId();
            var effectiveEntityId = sessionEntityId > 0
                ? (long?)sessionEntityId
                : (requestedEntityId.HasValue && requestedEntityId.Value > 0 ? requestedEntityId : (long?)null);

            if (!effectiveEntityId.HasValue || effectiveEntityId.Value <= 0)
            {
                return new List<Queue>();
            }

            var userId = auth.GetLoggedInWindowsUser();
            return _queues.ListByEntity(effectiveEntityId.Value, userId)
                .Where(q => q != null && q.ActiveFlag && !q.EmpOnly)
                .OrderBy(q => q.Name)
                .ToList();
        }

        public Result HandleProviderAction(string action, char srcType, long appointmentId, string providerId)
        {
            action = (action ?? string.Empty).Trim().ToLowerInvariant();
            return action switch
            {
                "arrive" => QueueCustomer(srcType, appointmentId, providerId),
                "begin" => BeginService(srcType, appointmentId, providerId),
                "end" => EndService(srcType, appointmentId, providerId),
                "remove" => RemoveAppointment(srcType, appointmentId, providerId),
                _ => Result.Fail("Unknown action")
            };
        }

        public sealed class TransferRequest
        {
            public char SrcType { get; set; }
            public long SrcId { get; set; }
            public long TargetQueueId { get; set; }
            public long? TargetServiceId { get; set; }
            public char TargetKind { get; set; }        // A(new appt) or W(new walkin)
            public DateTime? TargetDate { get; set; }   // required for A
            public DateTime? TargetEndDate { get; set; } // required for A
            //public string RefValue { get; set; }
            public string TargetNotes { get; set; }
            public string ServiceNotes { get; set; }
            public string StampUser { get; set; }
            public string SourceAction { get; set; }
        }

        public sealed class CloseAndAddRequest
        {
            public char SrcType { get; set; }
            public long SrcId { get; set; }
            public bool AdditionalService { get; set; }
            public long? TargetQueueId { get; set; }
            public long? TargetServiceId { get; set; }
            public char? TargetKind { get; set; }
            public DateTime? TargetDate { get; set; }
            public DateTime? TargetEndDate { get; set; } // required for A
            //public string RefValue { get; set; }
            public string TargetNotes { get; set; }
            public string ServiceNotes { get; set; }
            public string StampUser { get; set; }
        }

        public Result<long> TransferSource(TransferRequest request)
        {
            if (request == null) return Result<long>.Fail("Transfer request is required.");
            if (request.SrcId <= 0) return Result<long>.Fail("Source id is required.");
            if (request.TargetQueueId <= 0) return Result<long>.Fail("Target queue is required.");

            var srcType = char.ToUpperInvariant(request.SrcType);
            if (srcType != 'A' && srcType != 'W') return Result<long>.Fail("Source type must be A or W.");
            var sourceAction = string.IsNullOrWhiteSpace(request.SourceAction) ? "TRANSFER" : request.SourceAction.Trim().ToUpperInvariant();
            if (sourceAction != "TRANSFER" && sourceAction != "REMOVE" && sourceAction != "END")
                return Result<long>.Fail("Source action must be TRANSFER, REMOVE, or END.");

            var targetKind = char.ToUpperInvariant(request.TargetKind);
            if (targetKind != 'A' && targetKind != 'W') return Result<long>.Fail("Target kind must be A or W.");
            if (targetKind == 'A' && !request.TargetDate.HasValue)
                return Result<long>.Fail("Target date is required for appointment transfer.");

            var targetQueue = _queues.Get(request.TargetQueueId);
            if (targetQueue == null) return Result<long>.Fail("Target queue not found.");

            Appointment sourceAppt = null;
            if (srcType == 'A')
            {
                sourceAppt = _appts.Get(request.SrcId);
                if (sourceAppt == null) return Result<long>.Fail("Appointment not found.");
                if (sourceAppt.Status == AppointmentStatus.Completed || sourceAppt.Status == AppointmentStatus.Cancelled || sourceAppt.Status == AppointmentStatus.ClosedBySystem)
                    return Result<long>.Fail("Cannot transfer a finished appointment.");
            }

            var stampUser = string.IsNullOrWhiteSpace(request.StampUser) ? "web" : request.StampUser.Trim();
            var newSrcId = _serviceTransactions.TransferSource(
                srcType,
                request.SrcId,
                request.TargetQueueId,
                request.TargetServiceId,
                targetKind,
                request.TargetDate,
                request.TargetEndDate,
                //request.RefValue,
                request.TargetNotes,
                request.ServiceNotes,
                stampUser,
                sourceAction);

            if (sourceAppt != null)
            {
                sourceAppt.Status = sourceAction == "REMOVE"
                    ? AppointmentStatus.Cancelled
                    : (sourceAction == "END" ? AppointmentStatus.Completed : AppointmentStatus.TransferredOut);
                sourceAppt.UpdatedOn = DateTime.Now;
                sourceAppt.StampDate = DateTime.Now;
                PopulateCustomerNotificationFields(sourceAppt);
                _rt.AppointmentChanged(sourceAppt);
                _rt.QueueChanged(sourceAppt.EntityId, sourceAppt.QueueId);
            }
            else
            {
                var sourceStatus = sourceAction == "REMOVE"
                    ? AppointmentStatus.Cancelled
                    : (sourceAction == "END" ? AppointmentStatus.Completed : AppointmentStatus.TransferredOut);
                NotifySourceChanged(srcType, request.SrcId, sourceStatus, stampUser);
            }

            _rt.QueueChanged(targetQueue.EntityId, targetQueue.Id);
            return Result<long>.Success(newSrcId);
        }

        public Result<long> EndServiceAndOptionallyAdd(CloseAndAddRequest request)
        {
            if (request == null) return Result<long>.Fail("Request is required.");
            if (request.SrcId <= 0) return Result<long>.Fail("Source id is required.");

            var srcType = char.ToUpperInvariant(request.SrcType);
            if (srcType != 'A' && srcType != 'W') return Result<long>.Fail("Source type must be A or W.");

            if (request.AdditionalService)
            {
                if (!request.TargetQueueId.HasValue || request.TargetQueueId.Value <= 0)
                    return Result<long>.Fail("Target queue is required.");
                if (!request.TargetKind.HasValue)
                    return Result<long>.Fail("Target kind is required.");

                var targetKind = char.ToUpperInvariant(request.TargetKind.Value);
                if (targetKind != 'A' && targetKind != 'W')
                    return Result<long>.Fail("Target kind must be A or W.");
                if (targetKind == 'A' && !request.TargetDate.HasValue)
                    return Result<long>.Fail("Target date is required for appointment target.");
            }

            var stampUser = string.IsNullOrWhiteSpace(request.StampUser) ? "web" : request.StampUser.Trim();
            var newSrcId = _serviceTransactions.CloseAndAddSource(
                srcType,
                request.SrcId,
                request.AdditionalService,
                request.TargetQueueId,
                request.TargetServiceId,
                request.TargetKind,
                request.TargetDate,
                request.TargetEndDate,
                //request.RefValue,
                request.TargetNotes,
                request.ServiceNotes,
                stampUser);

            if (srcType == 'A')
            {
                var appt = _appts.Get(request.SrcId);
                if (appt != null)
                {
                    appt.Status = AppointmentStatus.Completed;
                    appt.ProviderId = request.StampUser;
                    appt.UpdatedOn = DateTime.Now; //_clock.UtcNow;
                    appt.StampDate = DateTime.Now; //_clock.UtcNow;
                    PopulateCustomerNotificationFields(appt);
                    _rt.AppointmentChanged(appt);
                    _rt.QueueChanged(appt.EntityId, appt.QueueId);
                }
            }
            else
            {
                NotifySourceChanged(srcType, request.SrcId, AppointmentStatus.Completed, stampUser);
            }

            if (request.TargetQueueId.HasValue && request.TargetQueueId.Value > 0)
            {
                var targetQueue = _queues.Get(request.TargetQueueId.Value);
                if (targetQueue != null)
                {
                    _rt.QueueChanged(targetQueue.EntityId, targetQueue.Id);
                }
            }

            return Result<long>.Success(newSrcId);
        }

        private Result QueueCustomer(char srcType, long appointmentId, string providerId)
        {
            var upperSrc = char.ToUpperInvariant(srcType);
            Appointment appt = null;
            if (upperSrc == 'A')
            {
                appt = _appts.Get(appointmentId);
                if (appt == null) return Result.Fail("Appointment not found.");
            }

            if (appt != null && (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.ClosedBySystem))
                return Result.Fail("Cannot queue a finished appointment.");

            var now = DateTime.Now;  // _clock.UtcNow;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();
            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "CHECKIN", stampUser, null);

            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.Arrived;
                appt.ProviderId = providerId;
                appt.UpdatedOn = now;
                appt.StampDate = now;

                PopulateCustomerNotificationFields(appt);
                _rt.AppointmentChanged(appt);
                _rt.QueueChanged(appt.EntityId, appt.QueueId);
            }
            else
            {
                NotifySourceChanged(srcType, appointmentId, AppointmentStatus.Arrived, providerId);
            }

            return Result.Success();
        }

        private Result BeginService(char srcType, long appointmentId, string providerId)
        {
            var upperSrc = char.ToUpperInvariant(srcType);
            Appointment appt = null;
            if (upperSrc == 'A')
            {
                appt = _appts.Get(appointmentId);
                if (appt == null) return Result.Fail("Appointment not found.");
            }

            if (appt != null && appt.Status != AppointmentStatus.Arrived && appt.Status != AppointmentStatus.Scheduled)
                return Result.Fail("Appointment must be queued or scheduled to begin service.");

            var now = DateTime.Now;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();

            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "START", stampUser, null);
            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.InService;
                appt.ProviderId = providerId;
                appt.UpdatedOn = now;
                appt.StampDate = now;

                PopulateCustomerNotificationFields(appt);
                _rt.AppointmentChanged(appt);
                _rt.QueueChanged(appt.EntityId, appt.QueueId);
            }
            else
            {
                NotifySourceChanged(srcType, appointmentId, AppointmentStatus.InService, providerId);
            }

            return Result.Success();
        }

        private Result EndService(char srcType, long appointmentId, string providerId)
        {
            var upperSrc = char.ToUpperInvariant(srcType);
            Appointment appt = null;
            if (upperSrc == 'A')
            {
                appt = _appts.Get(appointmentId);
                if (appt == null) return Result.Fail("Appointment not found.");
            }

            if (appt != null && appt.Status != AppointmentStatus.InService)
                return Result.Fail("Appointment must be in service to end service.");

            var now = DateTime.Now;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();
            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "END", stampUser, null);
            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.Completed;
                //appt.EndTime = DateTime.Now.TimeOfDay; // use ServiceTransRecord for reporting
                appt.ProviderId = providerId;
                appt.UpdatedOn = now;
                appt.StampDate = now;

                PopulateCustomerNotificationFields(appt);
                _rt.AppointmentChanged(appt);
                _rt.QueueChanged(appt.EntityId, appt.QueueId);
            }
            else
            {
                NotifySourceChanged(srcType, appointmentId, AppointmentStatus.Completed, providerId);
            }

            return Result.Success();
        }

        private Result RemoveAppointment(char srcType, long appointmentId, string providerId)
        {
            var upperSrc = char.ToUpperInvariant(srcType);
            Appointment appt = null;
            if (upperSrc == 'A')
            {
                appt = _appts.Get(appointmentId);
                if (appt == null) return Result.Fail("Appointment not found.");
            }

            if (appt != null && (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.ClosedBySystem))
                return Result.Fail("Appointment is already finished.");
            
            var now = DateTime.Now;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();

            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "REMOVE", stampUser, null);
            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.Cancelled;
                appt.ProviderId = providerId;
                appt.UpdatedOn = now;
                appt.StampDate = now;

                PopulateCustomerNotificationFields(appt);
                _rt.AppointmentChanged(appt);
                _rt.QueueChanged(appt.EntityId, appt.QueueId);
            }
            else
            {
                NotifySourceChanged(srcType, appointmentId, AppointmentStatus.Cancelled, providerId);
            }

            return Result.Success();
        }

        private void NotifySourceChanged(char srcType, long sourceId, AppointmentStatus status, string providerId)
        {
            var queueId = _appts.GetQueueIdForSource(srcType, sourceId);
            if (!queueId.HasValue || queueId.Value <= 0)
            {
                Trace.TraceWarning("NotifySourceChanged skipped: sourceType={0}, sourceId={1}, status={2}, queueId not found.", srcType, sourceId, status);
                return;
            }

            var queue = _queues.Get(queueId.Value);
            if (queue == null)
            {
                Trace.TraceWarning("NotifySourceChanged skipped: sourceType={0}, sourceId={1}, status={2}, queueId={3}, queue not found.", srcType, sourceId, status, queueId.Value);
                return;
            }

            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();
            var changed = new Appointment
            {
                Id = sourceId,
                EntityId = queue.EntityId,
                QueueId = queue.Id,
                Status = status,
                ProviderId = stampUser,
                StampUser = stampUser,
                UpdatedOn = DateTime.Now,
                StampDate = DateTime.Now
            };

            _rt.AppointmentChanged(changed);
            _rt.QueueChanged(changed.EntityId, changed.QueueId);
        }

        private static JObject ParseJsonObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JObject.Parse(json);
        }

        private static IList<QueueLookupOption> ReadOptions(JToken token, string codeField, string nameField)
        {
            var list = new List<QueueLookupOption>();
            var array = token as JArray;
            if (array == null)
            {
                return list;
            }

            foreach (var item in array.OfType<JObject>())
            {
                var code = item[codeField]?.ToString() ?? string.Empty;
                var name = item[nameField]?.ToString() ?? code;
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                list.Add(new QueueLookupOption
                {
                    Code = code,
                    Name = name
                });
            }

            return list;
        }

        private static IList<QueueScheduleOption> ReadSchedules(JToken token)
        {
            var list = new List<QueueScheduleOption>();
            var array = token as JArray;
            if (array == null)
            {
                return list;
            }

            foreach (var item in array.OfType<JObject>())
            {
                list.Add(new QueueScheduleOption
                {
                    ScheduleId = item["schedule_id"]?.ToObject<long>() ?? 0,
                    DateBegin = item["date_begin"]?.ToString() ?? string.Empty,
                    DateEnd = item["date_end"]?.ToString() ?? string.Empty,
                    OpenTime = item["open_time"]?.ToString() ?? string.Empty,
                    CloseTime = item["close_time"]?.ToString() ?? string.Empty,
                    IntervalTime = item["interval_time"]?.ToString() ?? string.Empty,
                    WeeklySchedule = item["weekly_sch"]?.ToString() ?? string.Empty,
                    AvailableResources = item["available_resources"]?.ToObject<int?>() ?? 0
                });
            }

            return list;
        }

        public sealed class QueueDetailOptions
        {
            public long QueueId { get; set; }
            public IList<QueueLookupOption> Services { get; set; } = new List<QueueLookupOption>();
            public IList<QueueLookupOption> ContactOptions { get; set; } = new List<QueueLookupOption>();
            public IList<QueueLookupOption> RefOptions { get; set; } = new List<QueueLookupOption>();
            public IList<QueueScheduleOption> Schedules { get; set; } = new List<QueueScheduleOption>();
        }

        public sealed class QueueLookupOption
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        public sealed class QueueScheduleOption
        {
            public long ScheduleId { get; set; }
            public string DateBegin { get; set; }
            public string DateEnd { get; set; }
            public string OpenTime { get; set; }
            public string CloseTime { get; set; }
            public string IntervalTime { get; set; }
            public string WeeklySchedule { get; set; }
            public int AvailableResources { get; set; }
        }
    }
}
