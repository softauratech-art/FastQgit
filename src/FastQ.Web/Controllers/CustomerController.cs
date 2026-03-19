using System;
using System.Globalization;
using System.Linq;
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

            if (!TryParseStartTime(startTime, out var parsedTime))
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

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public JsonResult ValidateReference(string referenceType, string enterValue, string streetNumber, string streetName, string streetType)
        {
            var res = _service.ValidateReference(referenceType, enterValue, streetNumber, streetName, streetType);
            return Json(new { success = res.Ok, message = res.Ok ? "Validation Succeeded..." : res.Error, ok = res.Ok, error = res.Ok ? null : res.Error }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetQueueOpenSlots(string queueId, string theDate)
        {
            if (!long.TryParse(queueId, out var parsedQueueId) || parsedQueueId <= 0)
                return Json(new { ok = false, error = "queueId is required" }, JsonRequestBehavior.AllowGet);
            if (!DateTime.TryParseExact((theDate ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                return Json(new { ok = false, error = "theDate is required" }, JsonRequestBehavior.AllowGet);

            var slots = _service.GetQueueOpenSlots(parsedQueueId, parsedDate)
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
            var res = _service.ValidateCustomerTimeSelection(email, phone, localStart.ToUniversalTime());
            return Json(new { ok = res.Ok, error = res.Ok ? null : res.Error }, JsonRequestBehavior.AllowGet);
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
