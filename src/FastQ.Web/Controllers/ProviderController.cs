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

        public ProviderController()
        {
            _service = new ProviderService();
        }

        [HttpGet]
        public ActionResult Today()
        {
            var today = DateTime.UtcNow.Date;

            var queues = _service.ListQueues().ToList();
            var customers = _service.ListCustomers().ToList();
            var appointments = _service.ListAppointmentsForDate(today).ToList();

            var queueMap = queues.ToDictionary(q => q.Id, q => q);
            var customerMap = customers.ToDictionary(c => c.Id, c => c);

            var rows = _service.BuildRows(appointments, queueMap, customerMap);

            var model = new ProviderTodayViewModel
            {
                DateText = DateTime.UtcNow.ToString("ddd, MMM dd yyyy", CultureInfo.InvariantCulture) + " (UTC)",
                LiveQueue = rows.Where(r => r.Status == AppointmentStatus.Arrived || r.Status == AppointmentStatus.InService).ToList(),
                Scheduled = rows.Where(r => r.Status != AppointmentStatus.Arrived && r.Status != AppointmentStatus.InService).ToList()
            };

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
            if (!Guid.TryParse(providerId, out var provId))
                provId = Guid.Parse("02a62b1c-0000-0000-4641-535451494430");

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

