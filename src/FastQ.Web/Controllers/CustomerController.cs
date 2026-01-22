using System;
using System.Web.Mvc;
using FastQ.Web.Services;

namespace FastQ.Web.Controllers
{
    public class CustomerController : Controller
    {
        private readonly CustomerService _service;

        public CustomerController()
        {
            _service = new CustomerService();
        }

        private static readonly Guid DefaultLocationId = new Guid("00a98ac7-0000-0000-4641-535451494430");

        [HttpGet]
        public ActionResult Book()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Book(string queueId, string phone, string firstName, string lastName)
        {
            if (!Guid.TryParse(queueId, out var qId))
            {
                ViewBag.Error = "Queue is required.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                ViewBag.Error = "First name, last name, and mobile number are required.";
                return View();
            }

            var name = ($"{firstName} {lastName}").Trim();
            var res = _service.BookFirstAvailable(DefaultLocationId, qId, phone, true, name);
            if (!res.Ok)
            {
                ViewBag.Error = res.Error;
                return View();
            }

            return Redirect($"/Customer/Home?appointmentId={Uri.EscapeDataString(res.Value.Id.ToString())}");
        }

        [HttpGet]
        public ActionResult Home()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Status(string appointmentId)
        {
            ViewBag.AppointmentId = (appointmentId ?? string.Empty).Trim();
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetAppointmentSnapshot(string appointmentId)
        {
            if (!Guid.TryParse(appointmentId, out var apptId))
                return Json(new { ok = false, error = "appointmentId is required" }, JsonRequestBehavior.AllowGet);

            var dto = _service.GetAppointmentSnapshot(apptId);
            if (dto == null)
                return Json(new { ok = false, error = "appointment not found" }, JsonRequestBehavior.AllowGet);

            return Json(new { ok = true, data = dto }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CancelAppointment(string appointmentId)
        {
            if (!Guid.TryParse(appointmentId, out var apptId))
                return Json(new { ok = false, error = "appointmentId is required" });

            var res = _service.Cancel(apptId);
            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true });
        }
    }
}
