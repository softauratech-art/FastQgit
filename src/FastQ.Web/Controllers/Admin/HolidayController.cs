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
                lHolidays = _service.ListHolidays();
            }
            catch (Exception ex) 
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("Error");
            }
            return View("../Admin/Holiday/List", lHolidays);
        }

        // GET: Holiday/Details/2026-01-01        
        public ActionResult Details(string day)
        {
            try 
            {
                if(DateTime.TryParse(day, out var dt))
                {
                    var oHoliday = _service.GetHoliday(dt);                
                    return View("../Admin/User/ManageUser", oHoliday);
                }
                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("Error");
            }            
        }

        // GET: User/Create        
        public ActionResult Create()
        {            
            var oUser = new UserVM { UserId = "", IsActive = true };
            return View("../Admin/Holiday/ManageHoliday", oUser);
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection collection, UserVM ovm)
        {
            return AddOrUpdateHoliday("A", collection, ovm);
        }

        // GET: User/Edit/ocuser01        
        public ActionResult Edit(string id)
        {                       
            //var oUser = _service.GetUser(id);
            return View("../Admin/Holiday/ManageHoliday");
        }

        // POST: User/Edit/ocuser01
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, FormCollection collection, UserVM ovm)
        {
            return AddOrUpdateHoliday("U", collection, ovm);
        }

        private ActionResult AddOrUpdateHoliday(string action, FormCollection collection, UserVM ovm)
        {
            return null;

            return View("../Admin/User/ManageUser", ovm);            
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

        private bool IsValid(string action, FormCollection fc)
        {
            string[] requiredflds = ["UserId", "FirstName", "LastName", "Email" ];    
            foreach (string flds in requiredflds)
            {
                if (string.IsNullOrEmpty(fc[flds]?.Trim()))
                {
                    ViewBag.ErrorMessage = $"{string.Join(", ", requiredflds)} are required.";
                    return false;
                }
            }

            //check fld-permissions too - for Create New User only
            if (action == "A")
            {
                string[] requiredperms = ["host", "queueadmin", "provider", "reporter"];               
                foreach (string flds in requiredperms)
                {
                    if (!string.IsNullOrEmpty(fc[flds]?.Trim()))
                        return true;                    
                }

                ViewBag.ErrorMessage = "At least one permission is required.";
                return false;
            }
            return true;
        }

    }
}
