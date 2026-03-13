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

        [HttpGet]
        public ActionResult Book()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Book(string queueId, string serviceId, string refValue, string email, string phone, string firstName, string lastName, string customerName, string contactType, string appointmentDate, string startTime, string permitNumber, string streetNumber, string streetName, string streetType, string meetingUrl, string notes)
        {
            if (!long.TryParse(queueId, out var qId))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Queue is required." });
                }
                ViewBag.Error = "Queue is required.";
                return View();
            }

            var resolvedCustomerName = string.IsNullOrWhiteSpace(customerName)
                ? ((firstName ?? string.Empty).Trim() + " " + (lastName ?? string.Empty).Trim()).Trim()
                : customerName.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(resolvedCustomerName))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Email, first name, last name, and mobile number are required." });
                }
                ViewBag.Error = "Email, first name, last name, and mobile number are required.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(serviceId))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Service is required." });
                }
                ViewBag.Error = "Service is required.";
                return View();
            }

            if (!DateTime.TryParseExact((appointmentDate ?? string.Empty).Trim(), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Appointment date is required." });
                }
                ViewBag.Error = "Appointment date is required.";
                return View();
            }

            if (!TimeSpan.TryParse((startTime ?? string.Empty).Trim(), out var parsedTime))
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = "Start time is required." });
                }
                ViewBag.Error = "Start time is required.";
                return View();
            }

            var localStart = DateTime.SpecifyKind(parsedDate.Date + parsedTime, DateTimeKind.Local);
            var res = _service.CreateScheduled(
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
                meetingUrl,
                "web");
            if (!res.Ok)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { ok = false, error = res.Error });
                }
                ViewBag.Error = res.Error;
                return View();
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new
                {
                    ok = true,
                    appointmentId = res.Value.Id,
                    redirectUrl = $"/Customer/Home?appointmentId={Uri.EscapeDataString(res.Value.Id.ToString())}"
                });
            }

            return Redirect($"/Customer/Home?appointmentId={Uri.EscapeDataString(res.Value.Id.ToString())}");
        }

        [HttpGet]
        public JsonResult LookupCustomerByEmail(string email)
        {
            var customer = _service.GetCustomerByEmail(email);
            if (customer == null)
            {
                return Json(new { ok = true, found = false }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                ok = true,
                found = true,
                data = new
                {
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
            var res = _service.ValidatePermit(permitNumber);
            return Json(
                new
                {
                    ok = res.Ok,
                    error = res.Ok ? null : res.Error
                },
                JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ValidateReference(string referenceType, string enterValue, string streetNumber, string streetName, string streetType)
        {
            var res = _service.ValidateReference(referenceType, enterValue, streetNumber, streetName, streetType);
            return Json(
                new
                {
                    ok = res.Ok,
                    error = res.Ok ? null : res.Error
                },
                JsonRequestBehavior.AllowGet);
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
            if (!long.TryParse(appointmentId, out var apptId))
                return Json(new { ok = false, error = "appointmentId is required" }, JsonRequestBehavior.AllowGet);

            var dto = _service.GetAppointmentSnapshot(apptId);
            if (dto == null)
                return Json(new { ok = false, error = "appointment not found" }, JsonRequestBehavior.AllowGet);

            return Json(new { ok = true, data = dto }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CancelAppointment(string appointmentId)
        {
            if (!long.TryParse(appointmentId, out var apptId))
                return Json(new { ok = false, error = "appointmentId is required" });

            var res = _service.Cancel(apptId);
            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true });
        }
    }
}
