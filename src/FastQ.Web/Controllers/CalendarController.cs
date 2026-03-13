using System;
using System.Globalization;
using System.Web.Mvc;
using FastQ.Web.Attributes;
using FastQ.Web.Helpers;
using FastQ.Web.Services;

namespace FastQ.Web.Controllers
{
    [FQAuthorizeUser(AllowRole = $"{nameof(Utilities.FQRole.Host)},{nameof(Utilities.FQRole.Provider)},{nameof(Utilities.FQRole.QueueAdmin)},{nameof(Utilities.FQRole.SuperAdmin)}")]
    public class CalendarController : Controller
    {
        private readonly CalendarService _service;
        private readonly AuthService _auth;

        public CalendarController()
        {
            _service = new CalendarService();
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

            if (!DateTime.TryParseExact(appointmentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Appointment date is required." });
                }
                return CalendarError(displayMonth, selected, "Appointment date is required.");
            }

            if (!TimeSpan.TryParse(startTime, CultureInfo.InvariantCulture, out var parsedTime))
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
                localStart.ToUniversalTime(),
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
    }
}
