using System;
using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using FastQ.Data.Common;
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
        public ActionResult Today(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                var resolvedUserId = _auth.GetLoggedInWindowsUser();
                if (!string.IsNullOrWhiteSpace(resolvedUserId))
                {
                    return RedirectToAction("Today", new { userId = resolvedUserId });
                }

                userId = resolvedUserId;
            }

            var today = DateTime.UtcNow.Date;
            var rows = string.IsNullOrWhiteSpace(userId)
                ? Enumerable.Empty<ProviderAppointmentRow>()
                : _service.BuildRowsForUser(userId, today);

            var model = new ProviderTodayViewModel
            {
                DateText = DateTime.UtcNow.ToString("ddd, MMM dd yyyy", CultureInfo.InvariantCulture) + " (UTC)",
                LiveQueue = rows.Where(r => r.Status == AppointmentStatus.Arrived || r.Status == AppointmentStatus.InService).ToList(),
                Scheduled = rows.Where(r => r.Status != AppointmentStatus.Arrived && r.Status != AppointmentStatus.InService).ToList()
            };

            ViewBag.ProviderId = userId ?? string.Empty;
            return View(model);
        }

        [HttpGet]
        public JsonResult GetQueueSnapshot(string locationId, string queueId)
        {
            if (!Guid.TryParse(locationId, out var locId) || !Guid.TryParse(queueId, out var qId))
                return Json(new { ok = false, error = "locationId and queueId are required" }, JsonRequestBehavior.AllowGet);

            var dto = _service.GetQueueSnapshot(locId, qId);
            return Json(new { ok = true, data = dto }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ProviderAction(string action, string appointmentId, string providerId)
        {
            if (string.IsNullOrWhiteSpace(action) || !Guid.TryParse(appointmentId, out var apptId))
                return Json(new { ok = false, error = "action and appointmentId are required" });

            action = action.Trim().ToLowerInvariant();
            Guid provId;
            if (!Guid.TryParse(providerId, out provId))
            {
                var resolvedUserId = _auth.GetLoggedInWindowsUser();
                if (!Guid.TryParse(resolvedUserId, out provId))
                    provId = Guid.Parse("02a62b1c-0000-0000-4641-535451494430");
            }

            var res = _service.HandleProviderAction(action, apptId, provId);

            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true });
        }

        [HttpPost]
        public JsonResult TransferAppointment(string appointmentId, string targetQueueId)
        {
            if (!Guid.TryParse(appointmentId, out var apptId) || !Guid.TryParse(targetQueueId, out var queueId))
                return Json(new { ok = false, error = "appointmentId and targetQueueId are required" });

            var res = _service.TransferAppointment(apptId, queueId);
            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true, newAppointmentId = res.Value.Id, newQueueId = res.Value.QueueId });
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

