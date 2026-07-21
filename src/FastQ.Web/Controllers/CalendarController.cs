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
        public ActionResult Index(string month, string selectedDate, string entry, string queue, string status)
        {
            var entityId = _auth.GetSessionEntityId();
            var userId = _auth.GetLoggedInWindowsUser();
            var displayMonth = ParseMonth(month);
            var selected = ParseDate(selectedDate) ?? DateTime.Today;
            if (selected.Year != displayMonth.Year || selected.Month != displayMonth.Month)
            {
                selected = new DateTime(displayMonth.Year, displayMonth.Month, Math.Min(selected.Day, DateTime.DaysInMonth(displayMonth.Year, displayMonth.Month)));
            }

            var model = _service.BuildCalendarModel(entityId, userId, displayMonth, selected, entry, queue, status);
            model.FeedbackMessage = TempData["CalendarMessage"] as string;
            model.FeedbackIsError = string.Equals(TempData["CalendarMessageIsError"] as string, "true", StringComparison.OrdinalIgnoreCase);
            ViewBag.ProviderId = userId ?? string.Empty;
            ViewBag.ServiceAccess = _auth.GetServicePageAccess();
            return View("~/Views/Provider/Calendar.cshtml", model);
        }

        private ActionResult CalendarError(DateTime displayMonth, DateTime selectedDate, string message)
        {
            var entityId = _auth.GetSessionEntityId();
            var userId = _auth.GetLoggedInWindowsUser();
            var model = _service.BuildCalendarModel(entityId, userId, displayMonth, selectedDate, null, null, null);
            model.FeedbackMessage = message;
            model.FeedbackIsError = true;
            ViewBag.ProviderId = userId ?? string.Empty;
            ViewBag.ServiceAccess = _auth.GetServicePageAccess();
            return View("~/Views/Provider/Calendar.cshtml", model);
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
