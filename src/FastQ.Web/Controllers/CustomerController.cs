using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
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

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public async Task<JsonResult> ValidateReference(string referenceType, string enterValue, string streetNumber, string streetName, string streetType)
        {
            try
            {
                if (string.IsNullOrEmpty(referenceType))
                {
                    return Json(new { success = false, message = "Reference type is required", ok = false, error = "Reference type is required" }, JsonRequestBehavior.AllowGet);
                }

                var apiBaseUrl = ConfigurationManager.AppSettings["FTAPIV1BaseUrl"];
                if (string.IsNullOrEmpty(apiBaseUrl))
                {
                    Trace.TraceError("FTAPIV1BaseUrl not configured in web.config");
                    return Json(new { success = false, message = "API configuration error", ok = false, error = "API configuration error" }, JsonRequestBehavior.AllowGet);
                }

                var apiKey = ConfigurationManager.AppSettings["FTApiKeyPolymorphic"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    Trace.TraceError("FTApiKeyPolymorphic not configured in web.config");
                    return Json(new { success = false, message = "API key configuration error", ok = false, error = "API key configuration error" }, JsonRequestBehavior.AllowGet);
                }

                apiBaseUrl = apiBaseUrl.TrimEnd('/');
                string apiUrl = null;
                var referenceTypeLower = referenceType.ToLowerInvariant();

                if (referenceTypeLower == "permit" || referenceTypeLower == "p")
                {
                    if (string.IsNullOrEmpty(enterValue))
                    {
                        return Json(new { success = false, message = "Permit number is required", ok = false, error = "Permit number is required" }, JsonRequestBehavior.AllowGet);
                    }
                    apiUrl = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", apiBaseUrl, enterValue.Trim());
                }
                else if (referenceTypeLower == "address" || referenceTypeLower == "a")
                {
                    if (string.IsNullOrEmpty(streetNumber))
                    {
                        return Json(new { success = false, message = "Street number is required", ok = false, error = "Street number is required" }, JsonRequestBehavior.AllowGet);
                    }
                    if (string.IsNullOrEmpty(streetName))
                    {
                        return Json(new { success = false, message = "Street name is required", ok = false, error = "Street name is required" }, JsonRequestBehavior.AllowGet);
                    }

                    var queryParams = new List<string>
                    {
                        "StreetNumber=" + Uri.EscapeDataString(streetNumber.Trim()),
                        "StreetName=" + Uri.EscapeDataString(streetName.Trim())
                    };
                    if (!string.IsNullOrEmpty(streetType))
                    {
                        queryParams.Add("StreetType=" + Uri.EscapeDataString(streetType.Trim()));
                    }

                    apiUrl = string.Format(CultureInfo.InvariantCulture, "{0}/search?{1}", apiBaseUrl, string.Join("&", queryParams));
                }
                else
                {
                    return Json(new { success = false, message = "Invalid reference type", ok = false, error = "Invalid reference type" }, JsonRequestBehavior.AllowGet);
                }

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    httpClient.DefaultRequestHeaders.Add("FTApiKeyPolymorphic", apiKey);

                    try
                    {
                        var response = await httpClient.GetAsync(apiUrl);
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            Trace.TraceInformation("API validation successful for {0}: {1}", referenceType, enterValue);
                            return Json(new { success = true, message = "Validation Succeeded...", ok = true, error = (string)null }, JsonRequestBehavior.AllowGet);
                        }

                        Trace.TraceWarning("API validation failed for {0}: {1}. Status: {2}, Response: {3}", referenceType, enterValue, response.StatusCode, responseContent);
                        var message = string.Format(CultureInfo.InvariantCulture, "Validation failed: {0}", response.StatusCode);
                        return Json(new { success = false, message = message, ok = false, error = message }, JsonRequestBehavior.AllowGet);
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Trace.TraceError("HTTP error calling API: {0}", httpEx.Message);
                        return Json(new { success = false, message = "Error connecting to validation service", ok = false, error = "Error connecting to validation service" }, JsonRequestBehavior.AllowGet);
                    }
                    catch (TaskCanceledException timeoutEx)
                    {
                        Trace.TraceError("Timeout calling API: {0}", timeoutEx.Message);
                        return Json(new { success = false, message = "Validation request timed out", ok = false, error = "Validation request timed out" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error validating reference: {0} - {1}", ex.Message, ex.StackTrace);
                return Json(new { success = false, message = "An error occurred during validation", ok = false, error = "An error occurred during validation" }, JsonRequestBehavior.AllowGet);
            }
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
