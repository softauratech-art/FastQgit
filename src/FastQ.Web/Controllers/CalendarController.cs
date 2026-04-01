using System;
using System.Globalization;
using System.Web.Mvc;
using FastQ.Web.Attributes;
using FastQ.Web.Helpers;
using FastQ.Web.Services;
using System.Diagnostics;
using System.Linq;

namespace FastQ.Web.Controllers
{
    [FQAuthorizeUser(AllowRole = $"{nameof(Utilities.FQRole.Host)},{nameof(Utilities.FQRole.Provider)},{nameof(Utilities.FQRole.QueueAdmin)},{nameof(Utilities.FQRole.SuperAdmin)}")]
    public class CalendarController : Controller
    {
        private readonly CalendarService _service;
        private readonly ProviderService _providerService;
        private readonly CustomerService _customerService;
        private readonly AuthService _auth;

        public CalendarController()
        {
            _service = new CalendarService();
            _providerService = new ProviderService();
            _customerService = new CustomerService();
            _auth = new AuthService();
        }

        [HttpGet]
        public ActionResult Index(string month, string selectedDate)
        {
            var userId = _auth.GetLoggedInWindowsUser();
            var displayMonth = ParseMonth(month);
            var selected = ParseDate(selectedDate) ?? DateTime.Today;
            if (selected.Year != displayMonth.Year || selected.Month != displayMonth.Month)
            {
                selected = new DateTime(displayMonth.Year, displayMonth.Month, Math.Min(selected.Day, DateTime.DaysInMonth(displayMonth.Year, displayMonth.Month)));
            }

            var model = _service.BuildCalendarModel(userId, displayMonth, selected);
            model.FeedbackMessage = TempData["CalendarMessage"] as string;
            model.FeedbackIsError = string.Equals(TempData["CalendarMessageIsError"] as string, "true", StringComparison.OrdinalIgnoreCase);
            ViewBag.ProviderId = userId ?? string.Empty;
            ViewBag.ServiceAccess = _auth.GetServicePageAccess();
            return View("~/Views/Admin/Calendar.cshtml", model);
        }

        [HttpGet]
        public JsonResult GetQueueDetails(string queueId)
        {
            if (!long.TryParse(queueId, out var parsedQueueId) || parsedQueueId <= 0)
                return Json(new { ok = false, error = "queueId is required" }, JsonRequestBehavior.AllowGet);

            var details = _providerService.GetQueueDetailOptions(parsedQueueId);
            if (details == null)
            {
                Trace.TraceWarning("Calendar.GetQueueDetails queueId={0}: service returned null details.", parsedQueueId);
                return Json(new { ok = false, error = "Queue details not found" }, JsonRequestBehavior.AllowGet);
            }

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
            if (!TryParseStartTime(startTime, out var parsedTime))
                return Json(new { ok = false, error = "Start time is required." }, JsonRequestBehavior.AllowGet);

            var localStart = DateTime.SpecifyKind(parsedDate.Date + parsedTime, DateTimeKind.Local);
            var res = _customerService.ValidateCustomerTimeSelection(email, phone, localStart);
            return Json(new { ok = res.Ok, error = res.Ok ? null : res.Error }, JsonRequestBehavior.AllowGet);
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

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public JsonResult ValidateReference(string referenceType, string enterValue, string streetNumber, string streetName, string streetType)
        {
            var res = _customerService.ValidateReference(referenceType, enterValue, streetNumber, streetName, streetType);
            return Json(new { success = res.Ok, message = res.Ok ? "Validation Succeeded..." : res.Error, ok = res.Ok, error = res.Ok ? null : res.Error }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAppointment(
            string queueId,
            string serviceId,
            string refValue,
            string permitNumber,
            string streetNumber,
            string streetName,
            string streetType,
            string email,
            string firstName,
            string lastName,
            string customerName,
            string phone,
            string contactType,
            string appointmentDate,
            string startTime,
            string languagePreference,
            string meetingUrl,
            string notes,
            string month)
        {
            var displayMonth = ParseMonth(month);
            var selected = ParseDate(appointmentDate) ?? displayMonth;

            if (!long.TryParse(queueId, out var qId))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Queue is required." });
                }
                return CalendarError(displayMonth, selected, "Queue is required.");
            }
            if (!_auth.CanAddEntries(qId))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "You do not have permission to add appointments." });
                }
                return CalendarError(displayMonth, selected, "You do not have permission to add appointments.");
            }

            var resolvedCustomerName = string.IsNullOrWhiteSpace(customerName)
                ? ((firstName ?? string.Empty).Trim() + " " + (lastName ?? string.Empty).Trim()).Trim()
                : customerName.Trim();
            if (string.IsNullOrWhiteSpace(resolvedCustomerName))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "First name and last name are required." });
                }
                return CalendarError(displayMonth, selected, "Customer name is required.");
            }

            if (string.IsNullOrWhiteSpace(phone))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Phone is required." });
                }
                return CalendarError(displayMonth, selected, "Phone is required.");
            }
            if (string.IsNullOrWhiteSpace(contactType))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Contact type is required." });
                }
                return CalendarError(displayMonth, selected, "Contact type is required.");
            }

            if (!DateTime.TryParseExact(appointmentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Appointment date is required." });
                }
                return CalendarError(displayMonth, selected, "Appointment date is required.");
            }

            if (!TryParseStartTime(startTime, out var parsedTime))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Start time is required." });
                }
                return CalendarError(displayMonth, parsedDate, "Start time is required.");
            }

            var localStart = DateTime.SpecifyKind(parsedDate.Date + parsedTime, DateTimeKind.Local);
            var res = _service.CreateScheduledAppointment(
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
                languagePreference,
                notes,
                meetingUrl);

            if (!res.Ok)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = res.Error });
                }
                return CalendarError(displayMonth, parsedDate, res.Error);
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new
                {
                    ok = true,
                    message = "Appointment added to the calendar.",
                    appointmentId = res.Value.Id,
                    month = parsedDate.ToString("yyyy-MM-01"),
                    selectedDate = parsedDate.ToString("yyyy-MM-dd")
                });
            }

            TempData["CalendarMessage"] = "Appointment added to the calendar.";
            TempData["CalendarMessageIsError"] = "false";

            return RedirectToAction("Index", new
            {
                month = parsedDate.ToString("yyyy-MM-01"),
                selectedDate = parsedDate.ToString("yyyy-MM-dd")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddWalkin(
            string queueId,
            string serviceId,
            string refValue,
            string permitNumber,
            string streetNumber,
            string streetName,
            string streetType,
            string email,
            string firstName,
            string lastName,
            string customerName,
            string phone,
            string contactType,
            string languagePreference,
            string meetingUrl,
            string notes,
            string month,
            string selectedDate)
        {
            var displayMonth = ParseMonth(month);
            var selected = ParseDate(selectedDate) ?? displayMonth;

            if (!long.TryParse(queueId, out var qId))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Queue is required." });
                }
                return CalendarError(displayMonth, selected, "Queue is required.");
            }
            if (!_auth.CanAddEntries(qId))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "You do not have permission to add walk-ins." });
                }
                return CalendarError(displayMonth, selected, "You do not have permission to add walk-ins.");
            }

            var resolvedCustomerName = string.IsNullOrWhiteSpace(customerName)
                ? ((firstName ?? string.Empty).Trim() + " " + (lastName ?? string.Empty).Trim()).Trim()
                : customerName.Trim();
            if (string.IsNullOrWhiteSpace(resolvedCustomerName))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "First name and last name are required." });
                }
                return CalendarError(displayMonth, selected, "Customer name is required.");
            }
            if (string.IsNullOrWhiteSpace(contactType))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Contact type is required." });
                }
                return CalendarError(displayMonth, selected, "Contact type is required.");
            }

            var res = _service.CreateWalkin(
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
                notes);

            if (!res.Ok)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = res.Error });
                }
                return CalendarError(displayMonth, selected, res.Error);
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new
                {
                    ok = true,
                    message = "Walk-in added to the queue.",
                    walkinId = res.Value,
                    month = displayMonth.ToString("yyyy-MM-01"),
                    selectedDate = selected.ToString("yyyy-MM-dd")
                });
            }

            TempData["CalendarMessage"] = "Walk-in added to the queue.";
            TempData["CalendarMessageIsError"] = "false";

            return RedirectToAction("Index", new
            {
                month = displayMonth.ToString("yyyy-MM-01"),
                selectedDate = selected.ToString("yyyy-MM-dd")
            });
        }

        private ActionResult CalendarError(DateTime displayMonth, DateTime selectedDate, string message)
        {
            var userId = _auth.GetLoggedInWindowsUser();
            var model = _service.BuildCalendarModel(userId, displayMonth, selectedDate);
            model.FeedbackMessage = message;
            model.FeedbackIsError = true;
            ViewBag.ProviderId = userId ?? string.Empty;
            ViewBag.ServiceAccess = _auth.GetServicePageAccess();
            return View("~/Views/Admin/Calendar.cshtml", model);
        }

        private static DateTime ParseMonth(string month)
        {
            if (DateTime.TryParseExact(month, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return new DateTime(parsed.Year, parsed.Month, 1);
            }

            if (DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return new DateTime(parsed.Year, parsed.Month, 1);
            }

            var now = DateTime.Today;
            return new DateTime(now.Year, now.Month, 1);
        }

        private static DateTime? ParseDate(string value)
        {
            if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                return parsed.Date;
            }

            return null;
        }

        private static bool TryParseStartTime(string value, out TimeSpan parsedTime)
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
    }
}
