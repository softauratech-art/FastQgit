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
        private readonly IQueueRepository _queues;
        private readonly IServiceTransactionRepository _serviceTransactions;
        private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;

        public ProviderService()
            : this(
                DbRepositoryFactory.CreateAppointmentRepository(),
                DbRepositoryFactory.CreateQueueRepository(),
                DbRepositoryFactory.CreateServiceTransactionRepository(),
                new SystemClock(),
                new SignalRRealtimeNotifier())
        {
        }

        public ProviderService(
            IAppointmentRepository appts,
            IQueueRepository queues,
            IServiceTransactionRepository serviceTransactions,
            IClock clock,
            IRealtimeNotifier rt)
        {
            _appts = appts;
            _queues = queues;
            _serviceTransactions = serviceTransactions;
            _clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        public Result SaveServiceInfo(long appointmentId, char srcType, string webexUrl, string notes, string stampUser)
        {
            var srcId = appointmentId;

            var user = string.IsNullOrWhiteSpace(stampUser) ? "web" : stampUser.Trim();
            _serviceTransactions.SaveServiceInfo(srcType, srcId, webexUrl, notes, user);
            return Result.Success();
        }

        public IList<Queue> ListQueues()
        {
            //return _queues.ListAll();
            return _queues.ListByEntity(null, new AuthService().GetLoggedInWindowsUser());
        }

        public IList<Queue> ListTransferQueues(long? locationId)
        {
            if (locationId.HasValue && locationId.Value > 0)
            {
                return _queues.ListByEntity(locationId.Value, new AuthService().GetLoggedInWindowsUser());
            }

            return _queues.ListByEntity(null, new AuthService().GetLoggedInWindowsUser());
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

        public IList<ProviderAppointmentRow> BuildRowsForUser(string userId, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<ProviderAppointmentRow>();
            }

            var rangeStart = rangeStartUtc.Date;
            var rangeEnd = rangeEndUtc.Date;
            var rows = _appts.ListForUser(userId, rangeStart, rangeEnd);

            return BuildProviderRows(rows);
        }

        public IList<ProviderAppointmentRow> BuildWalkinsForUser(string userId, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<ProviderAppointmentRow>();
            }

            var rangeStart = rangeStartUtc.Date;
            var rangeEnd = rangeEndUtc.Date;
            var rows = _appts.ListWalkinsForUser(userId, rangeStart, rangeEnd);

            return BuildProviderRows(rows);
        }

        private static IList<ProviderAppointmentRow> BuildProviderRows(IList<ProviderAppointmentData> rows)
        {
            return rows.Select(r =>
            {
                var serviceType = !string.IsNullOrWhiteSpace(r.ServiceName)
                    ? r.ServiceName
                    : (!string.IsNullOrWhiteSpace(r.QueueName) ? $"Questions: {r.QueueName}" : "Questions: General");

                return new ProviderAppointmentRow
                {
                    AppointmentId = r.AppointmentId,
                    ScheduledForUtc = r.ScheduledForUtc,
                    StartTimeText = r.ScheduledForUtc.ToString("h:mm tt"),
                    StartDateText = r.ScheduledForUtc.ToString("MMM dd, yyyy"),
                    QueueName = r.QueueName ?? "Unknown Queue",
                    ServiceType = serviceType,
                    CustomerName = string.IsNullOrWhiteSpace(r.CustomerName) ? "Unknown" : r.CustomerName,
                    Phone = string.IsNullOrWhiteSpace(r.CustomerPhone) ? "-" : r.CustomerPhone,
                    Status = r.Status,
                    StatusText = GetStatusText(r.Status),
                    ContactMethod = r.SmsOptIn ? "Online" : "In-Person"
                };
            }).OrderBy(r => r.ScheduledForUtc).ToList();
        }

        private static string GetStatusText(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Arrived => "ARRIVED",
                AppointmentStatus.InService => "IN PROGRESS",
                AppointmentStatus.Completed => "DONE",
                AppointmentStatus.Cancelled => "REMOVED",
                _ => status.ToString().ToUpperInvariant()
            };
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
            public char TargetKind { get; set; } // A(new appt) or W(new walkin)
            public DateTime? TargetDateUtc { get; set; } // required for A
            public string RefValue { get; set; }
            public string Notes { get; set; }
            public string StampUser { get; set; }
        }

        public sealed class CloseAndAddRequest
        {
            public char SrcType { get; set; }
            public long SrcId { get; set; }
            public bool AdditionalService { get; set; }
            public long? TargetQueueId { get; set; }
            public long? TargetServiceId { get; set; }
            public char? TargetKind { get; set; }
            public DateTime? TargetDateUtc { get; set; }
            public string RefValue { get; set; }
            public string Notes { get; set; }
            public string StampUser { get; set; }
        }

        public Result<long> TransferSource(TransferRequest request)
        {
            if (request == null) return Result<long>.Fail("Transfer request is required.");
            if (request.SrcId <= 0) return Result<long>.Fail("Source id is required.");
            if (request.TargetQueueId <= 0) return Result<long>.Fail("Target queue is required.");

            var srcType = char.ToUpperInvariant(request.SrcType);
            if (srcType != 'A' && srcType != 'W') return Result<long>.Fail("Source type must be A or W.");

            var targetKind = char.ToUpperInvariant(request.TargetKind);
            if (targetKind != 'A' && targetKind != 'W') return Result<long>.Fail("Target kind must be A or W.");
            if (targetKind == 'A' && !request.TargetDateUtc.HasValue)
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
                request.TargetDateUtc,
                request.RefValue,
                request.Notes,
                stampUser,
                "TRANSFER");

            if (sourceAppt != null)
            {
                sourceAppt.Status = AppointmentStatus.TransferredOut;
                sourceAppt.ProviderId = stampUser;
                sourceAppt.StampUser = stampUser;
                sourceAppt.UpdatedUtc = _clock.UtcNow;
                sourceAppt.StampDateUtc = _clock.UtcNow;
                _rt.AppointmentChanged(sourceAppt, stampUser);
                _rt.QueueChanged(sourceAppt.LocationId, sourceAppt.QueueId, stampUser);
            }

            if (targetKind == 'A' && newSrcId > 0)
            {
                var targetAppt = _appts.Get(newSrcId);
                if (targetAppt != null)
                {
                    targetAppt.ProviderId = stampUser;
                    targetAppt.StampUser = stampUser;
                    _rt.AppointmentChanged(targetAppt, stampUser);
                }
            }

            _rt.QueueChanged(targetQueue.LocationId, targetQueue.Id, stampUser);
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
                if (targetKind == 'A' && !request.TargetDateUtc.HasValue)
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
                request.TargetDateUtc,
                request.RefValue,
                request.Notes,
                stampUser);

            if (srcType == 'A')
            {
                var appt = _appts.Get(request.SrcId);
                if (appt != null)
                {
                    appt.Status = AppointmentStatus.Completed;
                    appt.ProviderId = stampUser;
                    appt.StampUser = stampUser;
                    appt.UpdatedUtc = _clock.UtcNow;
                    appt.StampDateUtc = _clock.UtcNow;
                    _rt.AppointmentChanged(appt, stampUser);
                    _rt.QueueChanged(appt.LocationId, appt.QueueId, stampUser);
                }
            }

            if (request.TargetQueueId.HasValue && request.TargetQueueId.Value > 0)
            {
                if (request.AdditionalService && request.TargetKind.HasValue && char.ToUpperInvariant(request.TargetKind.Value) == 'A' && newSrcId > 0)
                {
                    var targetAppt = _appts.Get(newSrcId);
                    if (targetAppt != null)
                    {
                        targetAppt.ProviderId = stampUser;
                        targetAppt.StampUser = stampUser;
                        _rt.AppointmentChanged(targetAppt, stampUser);
                    }
                }

                var targetQueue = _queues.Get(request.TargetQueueId.Value);
                if (targetQueue != null)
                {
                    _rt.QueueChanged(targetQueue.LocationId, targetQueue.Id, stampUser);
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

            var now = _clock.UtcNow;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();
            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "CHECKIN", stampUser, null);

            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.Arrived;
                appt.ProviderId = providerId;
                appt.UpdatedUtc = now;
                appt.StampDateUtc = now;

                _rt.AppointmentChanged(appt, stampUser);
                _rt.QueueChanged(appt.LocationId, appt.QueueId, stampUser);
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

            var now = _clock.UtcNow;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();

            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "START", stampUser, null);
            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.InService;
                appt.ProviderId = providerId;
                appt.UpdatedUtc = now;
                appt.StampDateUtc = now;

                _rt.AppointmentChanged(appt, stampUser);
                _rt.QueueChanged(appt.LocationId, appt.QueueId, stampUser);
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

            var now = _clock.UtcNow;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();
            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "END", stampUser, null);
            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.Completed;
                appt.ProviderId = providerId;
                appt.UpdatedUtc = now;
                appt.StampDateUtc = now;

                _rt.AppointmentChanged(appt, stampUser);
                _rt.QueueChanged(appt.LocationId, appt.QueueId, stampUser);
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

            var now = _clock.UtcNow;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();

            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "REMOVE", stampUser, null);
            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.Cancelled;
                appt.ProviderId = providerId;
                appt.UpdatedUtc = now;
                appt.StampDateUtc = now;

                _rt.AppointmentChanged(appt, stampUser);
                _rt.QueueChanged(appt.LocationId, appt.QueueId, stampUser);
            }

            return Result.Success();
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
