using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FastQ.Data.Entities;
using FastQ.Web.Attributes;
using FastQ.Web.Models.Admin;
using FastQ.Web.Services;

namespace FastQ.Web.Controllers.Admin
{
    [FQAuthorizeUser(AllowRole = "SuperAdmin")]
    public class HolidayController : Controller
    {
        // GET: Holiday       
        private readonly HolidayService _service;
        public HolidayController()
        {
            _service = new HolidayService();
        }

        public ActionResult Index()
        {
            IList<Holiday> lHolidays;
            try
            {
                lHolidays = _service.ListHolidays().Where(h => h.Day >= DateTime.Today.AddDays(-30)).ToList();                
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("Error");
            }
            return View("../Admin/Holiday/List", lHolidays);
        }

        [HttpPost]
        public JsonResult AddOrUpdateHoliday(FormCollection collection)
        {
            if (!DateTime.TryParse(collection["HolidayDate"].ToString(), out var dt)
                || string.IsNullOrEmpty(collection["HolidayDesc"]?.ToString()))
                return Json(new { response = "error", message = "Date and Description are required" });

            Boolean.TryParse(collection["ActiveFlag"]?.ToString(), out bool isactive);
            try
            {
                Data.Entities.Holiday hvm = new Data.Entities.Holiday
                                            {
                                                Day = dt,
                                                Description = collection["HolidayDesc"]?.ToString(),
                                                ActiveFlag = isactive
                                            };
                _service.AddOrUpdateHoliday(hvm);
                return Json(new { response = "success", message = "Record saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { response = "error", message = ex.Message });
            }
        }

        // POST: Queue/DeleteSchedule/5
        [HttpPost]
        public JsonResult DeleteConfirmed(string day)
        {
            try
            {
                _service.Delete(day);
                return Json(new { response = "success", message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { response = "error", message = ex.Message });
            }
        }
    }
}
