using System;
using System.Web.Mvc;
using FastQ.Data.Common;
using FastQ.Web.App_Start;

namespace FastQ.Web.Controllers
{
    public class ProviderController : Controller
    {
        [HttpGet]
        public ActionResult Today()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetQueueSnapshot(string locationId, string queueId)
        {
            if (!Guid.TryParse(locationId, out var locId) || !Guid.TryParse(queueId, out var qId))
                return Json(new { ok = false, error = "locationId and queueId are required" }, JsonRequestBehavior.AllowGet);

            var dto = CompositionRoot.Queries.GetQueueSnapshot(locId, qId);
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

            var res = action switch
            {
                "arrive" => CompositionRoot.Provider.MarkArrived(apptId),
                "begin" => CompositionRoot.Provider.BeginService(apptId, provId),
                "end" => CompositionRoot.Provider.EndService(apptId),
                _ => Result.Fail("Unknown action")
            };

            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true });
        }

        [HttpPost]
        public JsonResult TransferAppointment(string appointmentId, string targetQueueId)
        {
            if (!Guid.TryParse(appointmentId, out var apptId) || !Guid.TryParse(targetQueueId, out var queueId))
                return Json(new { ok = false, error = "appointmentId and targetQueueId are required" });

            var res = CompositionRoot.Transfer.Transfer(apptId, queueId);
            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true, newAppointmentId = res.Value.Id, newQueueId = res.Value.QueueId });
        }

        [HttpPost]
        public JsonResult SystemClose(int staleHours)
        {
            var hours = staleHours <= 0 ? 12 : staleHours;
            var closed = CompositionRoot.SystemClose.CloseStaleScheduledAppointments(hours);
            return Json(new { ok = true, closed = closed });
        }
    }
}
