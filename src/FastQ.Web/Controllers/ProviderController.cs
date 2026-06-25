using FastQ.Data.Entities;
using FastQ.Web.Attributes;
using FastQ.Web.Models;
using FastQ.Web.Services;
using FastQ.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Threading.Tasks;

namespace FastQ.Web.Controllers
{
    [FQAuthorizeUser(AllowRole =  $"{nameof(Utilities.FQRole.Host)},{nameof(Utilities.FQRole.Provider)},{nameof(Utilities.FQRole.QueueAdmin)},{nameof(Utilities.FQRole.SuperAdmin)}")]
    public class ProviderController : BaseController
    {
        private readonly ProviderService _service;
        private readonly CustomerService _customerService;
        private readonly AuthService _auth;
 
        public ProviderController()
        {
            _service = new ProviderService();
            _customerService = new CustomerService();
            _auth = new AuthService();
        }

        [HttpGet]
        public ActionResult Today(string start, string end)
        {
            return BuildTodayView(start, end, true, true);
        }

        [HttpGet]
        public ActionResult Appointments(string start, string end)
        {
            return BuildTodayView(start, end, false, true);
        }

        [HttpGet]
        public ActionResult Walkins(string start, string end)
        {
            return BuildTodayView(start, end, true, false);
        }

        private ActionResult BuildTodayView(string start, string end, bool showWalkins, bool showAppointments)
        {
            long entityId = _auth.GetSessionEntityId();
            var userId = _auth.GetLoggedInWindowsUser();

            var rangeStart = ParseDateOrDefault(start, DateTime.Now.Date);  //ParseDateOrDefault(start, DateTime.UtcNow.Date);
            var rangeEnd = ParseDateOrDefault(end, rangeStart);
            if (rangeEnd < rangeStart)
            {
                rangeEnd = rangeStart;
            }

            var walkins = showWalkins && !string.IsNullOrWhiteSpace(userId)
                ? _service.BuildWalkinsForUser(entityId, userId, rangeStart, rangeEnd)
                : Enumerable.Empty<ProviderAppointmentRow>();
            var appointments = showAppointments && !string.IsNullOrWhiteSpace(userId)
                ? _service.BuildRowsForUser(entityId, userId, rangeStart, rangeEnd)
                : Enumerable.Empty<ProviderAppointmentRow>();

            //var dateText = rangeStart == rangeEnd
            //    ? rangeStart.ToString("ddd, MMM dd yyyy", CultureInfo.InvariantCulture) + " (UTC)"
            //    : string.Format(CultureInfo.InvariantCulture, "{0:ddd, MMM dd yyyy} - {1:ddd, MMM dd yyyy} (UTC)", rangeStart, rangeEnd);
            
            var dateText = rangeStart == rangeEnd
                            ? rangeStart.ToString("ddd, MMM dd yyyy", CultureInfo.InvariantCulture)
                            : string.Format(CultureInfo.InvariantCulture, "{0:ddd, MMM dd yyyy} - {1:ddd, MMM dd yyyy}", rangeStart, rangeEnd);

            var model = new ProviderTodayViewModel
            {
                DateText = dateText,
                Walkins = walkins.ToList(),
                Appointments = appointments.ToList()
            };

            ViewBag.ProviderId = userId ?? string.Empty;
            ViewBag.StartDate = rangeStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ViewBag.EndDate = rangeEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ViewBag.ShowWalkins = showWalkins;
            ViewBag.ShowAppointments = showAppointments;
            ViewBag.ServiceAccess = _auth.GetServicePageAccess();
            return View("Today", model);
        }

        private static DateTime ParseDateOrDefault(string input, DateTime fallback)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return fallback.Date;
            }

            var formats = new[]
            {
                "yyyy-MM-dd",
                "yyyy-M-d",
                "MM/dd/yyyy",
                "M/d/yyyy",
                "MM/dd/yy",
                "M/d/yy",
                "dd/MM/yyyy",
                "d/M/yyyy",
                "dd/MM/yy",
                "d/M/yy"
            };

            DateTime parsed;
            if (DateTime.TryParseExact(input.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed.Date;
            }

            return fallback.Date;
        }

        [HttpGet]
        public JsonResult GetQueueSnapshot(string entityId, string queueId)
        {
            if (!long.TryParse(entityId, out var parsedEntityId) || !long.TryParse(queueId, out var qId))
                return Json(new { ok = false, error = "entityId and queueId are required" }, JsonRequestBehavior.AllowGet);

            var dto = _service.GetQueueSnapshot(parsedEntityId, qId);
            return Json(new { ok = true, data = dto }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTransferQueues(string entityId)
        {
            entityId ??= _auth.GetSessionEntityId().ToString();

            long parsedEntityId;
            long? selectedEntityId = long.TryParse(entityId, out parsedEntityId) ? parsedEntityId : (long?)null;
            var queues = _service.ListTransferQueues(selectedEntityId)
                .Select(q => new
                {
                    code = q.Id.ToString(CultureInfo.InvariantCulture),
                    name = q.Name ?? string.Empty
                })
                .ToList();

            return Json(new { ok = true, data = queues }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTransferServices(string queueId)
        {
            long parsedQueueId;
            if (!long.TryParse(queueId, out parsedQueueId) || parsedQueueId <= 0)
                return Json(new { ok = false, error = "queueId is required" }, JsonRequestBehavior.AllowGet);

            var services = _service.ListTransferServices(parsedQueueId)
                .Select(s => new
                {
                    code = s.Item1.ToString(CultureInfo.InvariantCulture),
                    name = s.Item2 ?? string.Empty
                })
                .ToList();

            return Json(new { ok = true, data = services }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetQueueDetails(string queueId)
        {
            if (!long.TryParse(queueId, out var parsedQueueId) || parsedQueueId <= 0)
                return Json(new { ok = false, error = "queueId is required" }, JsonRequestBehavior.AllowGet);

            var details = _service.GetQueueDetailOptions(parsedQueueId);
            if (details == null)
            {
                Trace.TraceWarning("GetQueueDetails queueId={0}: service returned null details.", parsedQueueId);
                return Json(new { ok = false, error = "Queue details not found" }, JsonRequestBehavior.AllowGet);
            }

            Trace.TraceInformation(
                "GetQueueDetails queueId={0}: returning services={1}, contacts={2}, refs={3}, schedules={4}.",
                parsedQueueId,
                details.Services.Count,
                details.ContactOptions.Count,
                details.RefOptions.Count,
                details.Schedules.Count);

            return Json(new
            {
                ok = true,
                data = new
                {
                    queueId = details.QueueId,
                    services = details.Services.Select(s => new { code = s.Code, name = s.Name }).ToList(),
                    contactOptions = details.ContactOptions.Select(c => new { code = c.Code, name = c.Name }).ToList(),
                    refOptions = details.RefOptions.Select(r => new { code = r.Code, name = r.Name }).ToList(),
                    schedules = details.Schedules.Select(s => new
                    {
                        scheduleId = s.ScheduleId,
                        dateBegin = s.DateBegin,
                        dateEnd = s.DateEnd,
                        openTime = s.OpenTime,
                        closeTime = s.CloseTime,
                        intervalTime = s.IntervalTime,
                        weeklySch = s.WeeklySchedule,
                        availableResources = s.AvailableResources
                    }).ToList()
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetQueueOpenSlots(string queueId, string theDate)
        {
            if (!long.TryParse(queueId, out var parsedQueueId) || parsedQueueId <= 0)
                return Json(new { ok = false, error = "queueId is required" }, JsonRequestBehavior.AllowGet);
            if (!DateTime.TryParseExact((theDate ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                return Json(new { ok = false, error = "theDate is required" }, JsonRequestBehavior.AllowGet);

            var slots = _customerService.GetQueueOpenSlots(parsedQueueId, parsedDate)
                .OrderBy(s => s.SlotBegin)
                .Select(s => new
                {
                    theDate = s.TheDate.ToString("yyyy-MM-dd"),
                    queueId = s.QueueId,
                    slotBegin = s.SlotBegin,
                    slotEnd = s.SlotEnd,
                    weeklySch = s.WeeklySchedule,
                    intervalTime = s.IntervalTime,
                    availableResources = s.AvailableResources
                })
                .ToList();

            return Json(new { ok = true, data = slots }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ValidateCustomerTimeSelection(string email, string phone, string appointmentDate, string startTime)
        {
            if (!DateTime.TryParseExact((appointmentDate ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                return Json(new { ok = false, error = "Appointment date is required." }, JsonRequestBehavior.AllowGet);
            if (!TryParseTime(startTime, out var parsedTime))
                return Json(new { ok = false, error = "Start time is required." }, JsonRequestBehavior.AllowGet);

            var localStart = DateTime.SpecifyKind(parsedDate.Date + parsedTime, DateTimeKind.Local);
            var res = _customerService.ValidateCustomerTimeSelection(email, phone, localStart);
            return Json(new { ok = res.Ok, error = res.Ok ? null : res.Error }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ProviderAction(string action, string appointmentId, string providerId, string srcType, string notes)
        {
            if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(appointmentId))
                return Json(new { ok = false, error = "action and appointmentId are required" });

            action = action.Trim().ToLowerInvariant();
            if (!long.TryParse(appointmentId, out var apptId))
                return Json(new { ok = false, error = "appointmentId must be a number" });

            var normalizedSrc = string.IsNullOrWhiteSpace(srcType) ? "A" : srcType.Trim().ToUpperInvariant();
            if (normalizedSrc != "A" && normalizedSrc != "W")
                return Json(new { ok = false, error = "srcType must be A or W" });

            var sourceQueueId = _service.GetSourceQueueId(normalizedSrc[0], apptId);
            var permissionError = ValidateActionPermission(action, sourceQueueId);
            if (!string.IsNullOrWhiteSpace(permissionError))
                return Json(new { ok = false, error = permissionError });

            var resolvedUserId = _auth.GetLoggedInWindowsUser();


            if (action == "remove" && string.IsNullOrWhiteSpace(notes) )
            {
                return Json(new { ok = false, error = "cancellation reason is required" });
            }            
            
            var res = _service.HandleProviderAction(action, normalizedSrc[0], apptId, resolvedUserId);

            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            var warningMessages = new List<string>();
            if (!string.IsNullOrWhiteSpace(res.Warning))
                warningMessages.Add(res.Warning);

            if (action == "remove" && normalizedSrc == "A")
            {
                var emailRes = _customerService.SendCancellationEmail(apptId);
                if (!emailRes.Ok)
                    return Json(new { ok = false, error = emailRes.Error });
                if (!string.IsNullOrWhiteSpace(emailRes.Warning))
                    warningMessages.Add(emailRes.Warning);
            }

            if (action == "remove" && !string.IsNullOrWhiteSpace(notes))
            {
                var saveRes = _service.SaveServiceInfo(apptId, normalizedSrc[0], null, null, notes, resolvedUserId);
                if (!saveRes.Ok)
                    warningMessages.Add(saveRes.Error);
                else if (!string.IsNullOrWhiteSpace(saveRes.Warning))
                    warningMessages.Add(saveRes.Warning);
            }

            var warningText = warningMessages
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .ToList();

            return Json(new
            {
                ok = true,
                warning = warningText.Count > 0 ? string.Join(" ", warningText) : null
            });
        }

        [HttpPost]
        public JsonResult AddAppointment(string queueId, string serviceId, string refValue, string permitNumber, string streetNumber, string streetName, string streetType, string email, string firstName, string lastName, string customerName, string phone, string contactType, string appointmentDate, string startTime, string endTime, string languagePreference, string meetingUrl, string notes)
        {
            if (!long.TryParse(queueId, out var qId))
                return Json(new { ok = false, error = "Queue is required." });
            if (!_auth.CanAddEntries(qId))
                return Json(new { ok = false, error = "You do not have permission to add appointments." });

            var resolvedCustomerName = string.IsNullOrWhiteSpace(customerName)
                ? ((firstName ?? string.Empty).Trim() + " " + (lastName ?? string.Empty).Trim()).Trim()
                : customerName.Trim();
            if (string.IsNullOrWhiteSpace(resolvedCustomerName))
                return Json(new { ok = false, error = "First name and last name are required." });
            if (string.IsNullOrWhiteSpace(contactType))
                return Json(new { ok = false, error = "Contact type is required." });

            if (!DateTime.TryParseExact((appointmentDate ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                return Json(new { ok = false, error = "Appointment date is required." });

            if (!TryParseTime(startTime, out var parsedTime))
                return Json(new { ok = false, error = "Start time is required." });

            TryParseTime(endTime, out var parsedEndTime);
               
            var localStart = DateTime.SpecifyKind(parsedDate.Date + parsedTime, DateTimeKind.Local);
            
            var res = _customerService.CreateScheduled(
                qId,
                serviceId,
                refValue,
                permitNumber,
                streetNumber,
                streetName,
                streetType,
                email,
                resolvedCustomerName,
                phone,
                contactType,
                localStart,
                parsedEndTime,
                languagePreference,
                notes,
                meetingUrl,
                _auth.GetLoggedInWindowsUser());

            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true, id = res.Value.Id, warning = res.Warning });
        }

        [HttpPost]
        public JsonResult AddWalkin(string queueId, string serviceId, string refValue, string permitNumber, string streetNumber, string streetName, string streetType, string email, string firstName, string lastName, string customerName, string phone, string contactType, string languagePreference, string meetingUrl, string notes)
        {
            if (!long.TryParse(queueId, out var qId))
                return Json(new { ok = false, error = "Queue is required." });
            if (!_auth.CanAddEntries(qId))
                return Json(new { ok = false, error = "You do not have permission to add walk-ins." });

            var resolvedCustomerName = string.IsNullOrWhiteSpace(customerName)
                ? ((firstName ?? string.Empty).Trim() + " " + (lastName ?? string.Empty).Trim()).Trim()
                : customerName.Trim();
            if (string.IsNullOrWhiteSpace(resolvedCustomerName))
                return Json(new { ok = false, error = "First name and last name are required." });
            if (string.IsNullOrWhiteSpace(contactType))
                return Json(new { ok = false, error = "Contact type is required." });

            var res = _customerService.CreateWalkin(
                qId,
                serviceId,
                refValue,
                permitNumber,
                streetNumber,
                streetName,
                streetType,
                email,
                resolvedCustomerName,
                phone,
                contactType,
                languagePreference,
                meetingUrl,
                notes,
                _auth.GetLoggedInWindowsUser());

            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true, id = res.Value });
        }

        [HttpGet]
        public JsonResult GetCustomerByEmail(string email)
        {
            return LookupCustomerByEmail(email);
        }

        [HttpGet]
        public JsonResult LookupCustomerByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { ok = false, error = "email is required" }, JsonRequestBehavior.AllowGet);

            var customer = _customerService.GetCustomerByEmail(email);
            if (customer == null)
                return Json(new { ok = true, found = false }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                ok = true,
                found = true,
                data = new
                {
                    id = customer.Id,
                    firstName = customer.FirstName ?? string.Empty,
                    lastName = customer.LastName ?? string.Empty,
                    phone = customer.Phone ?? string.Empty,
                    email = customer.Email ?? string.Empty
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ValidatePermit(string permitNumber)
        {
            var res = _customerService.ValidatePermit(permitNumber);
            return Json(
                new
                {
                    ok = res.Ok,
                    error = res.Ok ? null : res.Error
                },
                JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public JsonResult ValidateReference(string referenceType, string enterValue, string streetNumber, string streetName, string streetType)
        {
            var res = _customerService.ValidateReference(referenceType, enterValue, streetNumber, streetName, streetType);
            return Json(new { success = res.Ok, message = res.Ok ? "Validation Succeeded..." : res.Error, ok = res.Ok, error = res.Ok ? null : res.Error }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult TransferAppointment(string appointmentId, string targetQueueId, string srcType, string targetKind, string targetServiceId, string targetDate, string targetEndDate, string targetNotes, string serviceNotes, string sourceAction, string customerEmail, string customerPhone)
        {
            if (!long.TryParse(appointmentId, out var srcId) || !long.TryParse(targetQueueId, out var queueId))
                return Json(new { ok = false, error = "appointmentId and targetQueueId are required numeric values" });

            var normalizedSrc = string.IsNullOrWhiteSpace(srcType) ? "A" : srcType.Trim().ToUpperInvariant();
            if (normalizedSrc != "A" && normalizedSrc != "W")
                return Json(new { ok = false, error = "srcType must be A or W" });
            var normalizedSourceAction = string.IsNullOrWhiteSpace(sourceAction) ? "TRANSFER" : sourceAction.Trim().ToUpperInvariant();
            if (normalizedSourceAction != "TRANSFER" && normalizedSourceAction != "REMOVE" && normalizedSourceAction != "END")
                return Json(new { ok = false, error = "sourceAction must be TRANSFER, REMOVE, or END" });
            var sourceQueueId = _service.GetSourceQueueId(normalizedSrc[0], srcId);
            var transferPermissionError = ValidateActionPermission("transfer", sourceQueueId);
            if (!string.IsNullOrWhiteSpace(transferPermissionError))
                return Json(new { ok = false, error = transferPermissionError });
            if (normalizedSourceAction == "REMOVE")
            {
                if (string.IsNullOrWhiteSpace(serviceNotes))
                    return Json(new { ok = false, error = "cancellation reason is required" });
           
                var removePermissionError = ValidateActionPermission("remove", sourceQueueId);
                if (!string.IsNullOrWhiteSpace(removePermissionError))
                    return Json(new { ok = false, error = removePermissionError });
            }
            else if (normalizedSourceAction == "END")
            {
                var endPermissionError = ValidateActionPermission("end", sourceQueueId);
                if (!string.IsNullOrWhiteSpace(endPermissionError))
                    return Json(new { ok = false, error = endPermissionError });
            }

            var normalizedTarget = string.IsNullOrWhiteSpace(targetKind) ? normalizedSrc : targetKind.Trim().ToUpperInvariant();
            if (normalizedTarget != "A" && normalizedTarget != "W")
                return Json(new { ok = false, error = "targetKind must be A or W" });

            long parsedTargetServiceId;
            long? targetService = long.TryParse(targetServiceId, out parsedTargetServiceId) ? parsedTargetServiceId : (long?)null;

            var parsedTargetDateValue = TryParseTransferTargetDate(targetDate, out var parsedTargetDate)
                ? parsedTargetDate
                : (DateTime?)null;

            if (normalizedTarget == "A" && parsedTargetDateValue.HasValue)
            {
                var excludedAppointmentId = normalizedSrc == "A" ? (long?)srcId : null;
                var customerTimeValidation = _customerService.ValidateCustomerTimeSelection(
                    customerEmail,
                    customerPhone,
                    parsedTargetDateValue.Value,
                    excludedAppointmentId,
                    true);
                if (!customerTimeValidation.Ok)
                    return Json(new { ok = false, error = customerTimeValidation.Error });
            }

            var parsedTargetEndDateValue = TryParseTransferTargetDate(targetEndDate, out var parsedTargetEndDate)
               ? parsedTargetEndDate
               : (DateTime?)null;

            var req = new ProviderService.TransferRequest
            {
                SrcType = normalizedSrc[0],
                SrcId = srcId,
                TargetQueueId = queueId,
                TargetServiceId = targetService,
                TargetKind = normalizedTarget[0],
                TargetDate = parsedTargetDateValue,
                TargetEndDate = parsedTargetEndDateValue,
                //RefValue = refValue,
                TargetNotes = targetNotes,
                ServiceNotes = serviceNotes,
                StampUser = _auth.GetLoggedInWindowsUser(),
                SourceAction = normalizedSourceAction
            };

            var res = _service.TransferSource(req);
            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true, newSrcId = res.Value, newQueueId = queueId, targetKind = normalizedTarget });
        }

        [HttpPost]
        public JsonResult EndService(string appointmentId, string srcType, string additionalService, string targetQueueId, string targetServiceId, string targetKind, string targetDate, string targetEndDate, string targetNotes, string completionNotes)
        {
            long srcId;
            if (!long.TryParse(appointmentId, out srcId))
                return Json(new { ok = false, error = "appointmentId must be numeric" });

            var normalizedSrc = string.IsNullOrWhiteSpace(srcType) ? "A" : srcType.Trim().ToUpperInvariant();
            if (normalizedSrc != "A" && normalizedSrc != "W")
                return Json(new { ok = false, error = "srcType must be A or W" });
            var sourceQueueId = _service.GetSourceQueueId(normalizedSrc[0], srcId);
            var endPermissionError = ValidateActionPermission("end", sourceQueueId);
            if (!string.IsNullOrWhiteSpace(endPermissionError))
                return Json(new { ok = false, error = endPermissionError });

            var wantsAdditional = string.Equals((additionalService ?? string.Empty).Trim(), "Y", StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(completionNotes))
                return Json(new { ok = false, error = "Notes are required." });

            long parsedQueue;
            long? queueId = long.TryParse(targetQueueId, out parsedQueue) ? parsedQueue : (long?)null;
            long parsedService;
            long? serviceId = long.TryParse(targetServiceId, out parsedService) ? parsedService : (long?)null;

            var normalizedTargetKind = string.IsNullOrWhiteSpace(targetKind) ? (string)null : targetKind.Trim().ToUpperInvariant();
            if (normalizedTargetKind != null && normalizedTargetKind != "A" && normalizedTargetKind != "W")
                return Json(new { ok = false, error = "targetKind must be A or W" });

            var parsedTargetDateValue = TryParseTransferTargetDate(targetDate, out var parsedTargetDate)
                ? parsedTargetDate
                : (DateTime?)null;

            var parsedTargetEndDateValue = TryParseTransferTargetDate(targetEndDate, out var parsedTargetEndDate)
                ? parsedTargetEndDate
                : (DateTime?)null;


            var req = new ProviderService.CloseAndAddRequest
            {
                SrcType = normalizedSrc[0],
                SrcId = srcId,
                AdditionalService = wantsAdditional,
                TargetQueueId = queueId,
                TargetServiceId = serviceId,
                TargetKind = string.IsNullOrWhiteSpace(normalizedTargetKind) ? (char?)null : normalizedTargetKind[0],
                TargetDate = parsedTargetDateValue,
                TargetEndDate = parsedTargetEndDateValue,
                //RefValue = refValue,
                TargetNotes = targetNotes,
                ServiceNotes = completionNotes,
                StampUser = _auth.GetLoggedInWindowsUser()
            };

            var res = _service.EndServiceAndOptionallyAdd(req);
            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            //if (!string.IsNullOrWhiteSpace(completionNotes))
            //{
            //    var saveRes = _service.SaveServiceInfo(srcId, normalizedSrc[0], null, null, completionNotes, _auth.GetLoggedInWindowsUser());
            //    if (!saveRes.Ok)
            //        return Json(new { ok = true, newSrcId = res.Value, warning = saveRes.Error });
            //}

            return Json(new { ok = true, newSrcId = res.Value });
        }

        [HttpPost]
        //public JsonResult SaveServiceInfo(string appointmentId, string srcType, string webexUrl, string guestUrl, string hostUrl, string notes, string providerId)
        public JsonResult SaveServiceInfo(string appointmentId, string srcType, string guestUrl, string hostUrl, string notes)
        {
            if (!long.TryParse(appointmentId, out var apptId))
                return Json(new { ok = false, error = "appointmentId must be a number" });

            var normalized = string.IsNullOrWhiteSpace(srcType) ? "A" : srcType.Trim().ToUpperInvariant();
            if (normalized != "A" && normalized != "W")
                return Json(new { ok = false, error = "srcType must be A or W" });
            var sourceQueueId = _service.GetSourceQueueId(normalized[0], apptId);
            var updatePermissionError = ValidateActionPermission("info", sourceQueueId);
            if (!string.IsNullOrWhiteSpace(updatePermissionError))
                return Json(new { ok = false, error = updatePermissionError });

            var resolvedUserId = _auth.GetLoggedInWindowsUser();
            var resolvedHostUrl = string.IsNullOrWhiteSpace(hostUrl) ? "" : hostUrl;
            var res = _service.SaveServiceInfo(apptId, normalized[0], guestUrl, resolvedHostUrl, notes, resolvedUserId);
            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true });
        }

        private string ValidateActionPermission(string action, long? queueId)
        {
            if (!queueId.HasValue || queueId.Value <= 0)
                return "Queue information could not be resolved for this item.";

            var normalizedAction = (action ?? string.Empty).Trim().ToLowerInvariant();
            var allowed = normalizedAction switch
            {
                "arrive" => _auth.CanAccessQueueActions(queueId.Value),
                "transfer" => _auth.CanAccessQueueActions(queueId.Value),
                "remove" => _auth.CanAccessQueueActions(queueId.Value),
                "info" => _auth.CanAccessQueueActions(queueId.Value),
                "begin" => _auth.CanAccessProviderServiceActions(queueId.Value),
                "end" => _auth.CanAccessProviderServiceActions(queueId.Value),
                _ => false
            };

            return allowed ? null : "You do not have permission for this action.";
        }

        private static bool TryParseTime(string value, out TimeSpan parsedTime)
        {
            var text = (value ?? string.Empty).Trim();
            if (TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out parsedTime))
            {
                return true;
            }

            if (DateTime.TryParseExact(text, new[] { "h:mm tt", "hh:mm tt" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDateTime))
            {
                parsedTime = parsedDateTime.TimeOfDay;
                return true;
            }

            parsedTime = default;
            return false;
        }

        private static bool TryParseTransferTargetDate(string value, out DateTime parsedDateTime)
        {
            var text = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                parsedDateTime = default;
                return false;
            }

            var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2
                && DateTime.TryParseExact(parts[0], new[] { "yyyy-MM-dd", "yyyy-M-d", "MM/dd/yyyy", "M/d/yyyy", "dd/MM/yyyy", "d/M/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
                && TryParseTime(string.Join(" ", parts.Skip(1)), out var parsedTime))
            {
                parsedDateTime = parsedDate.Date + parsedTime;
                return true;
            }

            if (DateTime.TryParseExact(
                text,
                new[]
                {
                    "yyyy-MM-dd HH:mm",
                    "yyyy-M-d H:mm",
                    "yyyy-MM-dd H:mm",
                    "yyyy-M-d HH:mm",
                    "yyyy-MM-dd h:mm tt",
                    "yyyy-M-d h:mm tt",
                    "yyyy-MM-dd hh:mm tt",
                    "yyyy-M-d hh:mm tt",
                    "yyyy-MM-dd",
                    "yyyy-M-d",
                    "MM/dd/yyyy HH:mm",
                    "M/d/yyyy H:mm",
                    "MM/dd/yyyy h:mm tt",
                    "M/d/yyyy h:mm tt",
                    "MM/dd/yyyy",
                    "M/d/yyyy",
                    "dd/MM/yyyy HH:mm",
                    "d/M/yyyy H:mm",
                    "dd/MM/yyyy h:mm tt",
                    "d/M/yyyy h:mm tt",
                    "dd/MM/yyyy",
                    "d/M/yyyy"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out parsedDateTime))
            {
                return true;
            }

            return false;
        }

        [HttpGet]
        public  async Task<string> JoinMeeting(string srctype, long srcid)
        {
            var webexSvc = new Services.WebexService();

            var response = await webexSvc.LaunchStartLink(srctype, srcid);
            
            if (response.ApiError != null)
                Response.Write(response.ApiError);
            else
                Response.Redirect(response.HostUrl);

            return null;
        }
    }
}
