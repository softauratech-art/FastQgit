using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using FastQ.Data.Entities;
using FastQ.Web.Models;
using FastQ.Web.Services;

namespace FastQ.Web.Controllers
{
    public class ProviderController : Controller
    {
        private readonly ProviderService _service;
        private readonly AuthService _auth;

        public ProviderController()
        {
            _service = new ProviderService();
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
            var userId = _auth.GetLoggedInWindowsUser();

            var rangeStart = ParseDateOrDefault(start, DateTime.Now.Date);  //ParseDateOrDefault(start, DateTime.UtcNow.Date);
            var rangeEnd = ParseDateOrDefault(end, rangeStart);
            if (rangeEnd < rangeStart)
            {
                rangeEnd = rangeStart;
            }

            var walkins = showWalkins && !string.IsNullOrWhiteSpace(userId)
                ? _service.BuildWalkinsForUser(userId, rangeStart, rangeEnd)
                : Enumerable.Empty<ProviderAppointmentRow>();
            var appointments = showAppointments && !string.IsNullOrWhiteSpace(userId)
                ? _service.BuildRowsForUser(userId, rangeStart, rangeEnd)
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
        public JsonResult GetQueueSnapshot(string locationId, string queueId)
        {
            if (!long.TryParse(locationId, out var locId) || !long.TryParse(queueId, out var qId))
                return Json(new { ok = false, error = "locationId and queueId are required" }, JsonRequestBehavior.AllowGet);

            var dto = _service.GetQueueSnapshot(locId, qId);
            return Json(new { ok = true, data = dto }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ProviderAction(string action, string appointmentId, string providerId, string srcType)
        {
            if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(appointmentId))
                return Json(new { ok = false, error = "action and appointmentId are required" });

            action = action.Trim().ToLowerInvariant();
            if (!long.TryParse(appointmentId, out var apptId))
                return Json(new { ok = false, error = "appointmentId must be a number" });

            var normalizedSrc = string.IsNullOrWhiteSpace(srcType) ? "A" : srcType.Trim().ToUpperInvariant();
            if (normalizedSrc != "A" && normalizedSrc != "W")
                return Json(new { ok = false, error = "srcType must be A or W" });

            var resolvedUserId = _auth.GetLoggedInWindowsUser();

            var res = _service.HandleProviderAction(action, normalizedSrc[0], apptId, resolvedUserId);

            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true });
        }

        [HttpPost]
        public JsonResult TransferAppointment(string appointmentId, string targetQueueId)
        {
            if (!long.TryParse(appointmentId, out var apptId) || !long.TryParse(targetQueueId, out var queueId))
                return Json(new { ok = false, error = "appointmentId and targetQueueId are required" });

            var res = _service.TransferAppointment(apptId, queueId);
            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true, newAppointmentId = res.Value.Id, newQueueId = res.Value.QueueId });
        }

        [HttpPost]
        public JsonResult SaveServiceInfo(string appointmentId, string srcType, string webexUrl, string notes, string providerId)
        {
            if (!long.TryParse(appointmentId, out var apptId))
                return Json(new { ok = false, error = "appointmentId must be a number" });

            var normalized = string.IsNullOrWhiteSpace(srcType) ? "A" : srcType.Trim().ToUpperInvariant();
            if (normalized != "A" && normalized != "W")
                return Json(new { ok = false, error = "srcType must be A or W" });

            var resolvedUserId = _auth.GetLoggedInWindowsUser();
            var res = _service.SaveServiceInfo(apptId, normalized[0], webexUrl, notes, resolvedUserId);
            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true });
        }

        [HttpPost]
        public JsonResult SystemClose(int staleHours)
        {
            var hours = staleHours <= 0 ? 12 : staleHours;
            var closed = _service.CloseStaleScheduledAppointments(hours);
            return Json(new { ok = true, closed = closed });
        }
    }
}

