using FastQ.Data.Common;
using FastQ.Web.Attributes;
using FastQ.Web.Models.Admin;
using FastQ.Web.Services;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace FastQ.Web.Controllers.Admin
{
    public class QueueController : Controller
    {
        #region Queue-Base-Record
        // GET: User       
        ////[AuthorizeUsers]        
        //[Route("admin")]
        public ActionResult Index()
        {
            IList<QueueVM> lQueues;
            try
            {
                lQueues = new QueueService().ListQueues();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("Error");
            }

            return View("../Admin/Queue/List", lQueues);
        }

        // GET: Queue/Details/1
        [AuthorizeUsers]
        //[Route("admin")]
        public ActionResult Details(long id)
        {
            try
            {
                var oQueue = new QueueService().GetQueue(id);
                if (oQueue == null)  return HttpNotFound();                 

                return View("../Admin/Queue/View", oQueue);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("Error");
            }
        }

        // GET: Queue/Create
        //[AuthorizeUsers] 
        public ActionResult Create()
        {
            //var locationid = Convert.ToInt64(Request.QueryString["locid"].ToString());
            var oQueue = new QueueVM { Id = 0, LocationId = 1, ActiveFlag=true };
            return View("../Admin/Queue/ManageQueue", oQueue);
        }

        // POST: Queue/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[AuthorizeUsers] 
        public ActionResult Create(QueueVM ovm)
        {
            try
            {
                // TODO: Add insert logic here

                return Edit(ovm);
            }
            catch
            {
                return View();
            }
        }

        // GET: Queue/Edit(Insert)
        // GET: Queue/Edit/5(Update)
        [AuthorizeUsers]
        public ActionResult Edit(long id = 0)
        {
            //If id=0 For-Insert
            if (id == 0)
                return View("../Admin/Queue/ManageQueue", new QueueVM { Id = 0, LocationId = 1 });

            //else For-Update
            var oQueue = new QueueService().GetQueue(id);
            oQueue.LeadTimeMin = Helpers.Utilities.ParseDurationFromISO(oQueue.LeadTimeMin);
            oQueue.LeadTimeMax = Helpers.Utilities.ParseDurationFromISO(oQueue.LeadTimeMax);

            return View("../Admin/Queue/ManageQueue", oQueue);
        }

        // POST: Queue/Edit/10001
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[AuthorizeUsers] 
        //public ActionResult Edit(string id, FormCollection collection)
        public ActionResult Edit(QueueVM ovm)
        {
            try
            {
                //set these from Form[] since we're using custom-checkbox in View-html
                ovm.ActiveFlag = Request.Form["ActiveFlag"] != null ? Request.Form["ActiveFlag"].Equals("true") : false;
                ovm.EmpOnly = Request.Form["EmpOnly"] != null ? Request.Form["EmpOnly"].Equals("true") : false;
                ovm.HideInKiosk = Request.Form["HideInKiosk"] != null ? Request.Form["HideInKiosk"].Equals("true") : false;
                ovm.HideInMonitor = Request.Form["HideInMonitor"] != null ? Request.Form["HideInMonitor"].Equals("true") : false;
                ovm.HasGuidelines = Request.Form["HasGuidelines"] != null ? Request.Form["HasGuidelines"].Equals("true") : false;
                ovm.HasUploads = Request.Form["HasUploads"] != null ? Request.Form["HasUploads"].Equals("true") : false;

                if (ModelState.IsValid)
                {
                    Int64 id = new QueueService().AddOrUpdateQueue(ovm);
                    ViewBag.SuccessMessage = "Data saved successfully";

                    // if no errors then send to Details-view
                    var oQueue = new QueueService().GetQueue(id);
                    if (oQueue == null) return HttpNotFound();
                    
                    if (ovm.Id == 0)
                        return RedirectToAction("Edit", "Queue", new { id = oQueue.Id });
                    else
                        return View("../Admin/Queue/ManageQueue", oQueue);                    
                }

                ViewBag.ErrorMessage = "Validation failed. Please check the details.";
                return View("../Admin/Queue/ManageQueue", ovm);
            }
            catch (Exception ex) 
            {
                ViewBag.ErrorMessage = "An error occurred while processing your request. " + ex.Message;
                return View("../Admin/Queue/ManageQueue", ovm);
            }
        }

        // GET: Queue/Delete/10001
        //[AuthorizeUsers] 
        public ActionResult Delete(Int64 id)
        {
            try
            {
                new QueueService().Delete(id);
                ViewBag.SuccessMessage = "Record deleted successfully";
            }
            catch (Exception ex){
                ViewBag.ErrorMessage = "An error occurred while processing your request. " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: Queue/Delete/5
        [HttpPost]
        //[AuthorizeUsers] 
        public ActionResult Delete(Int64 id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        #endregion Queue-Base-Record

        #region Schedule actions
        // GET: Queue/EditSchedule(Insert)
        // GET: Queue/EditSchedule/5(Update)
        [HttpGet]
        //[AuthorizeUsers] 
        public ActionResult EditSchedule(long id = 0)
        {
            QueueScheduleVM ovm = new QueueService().GetQueueSchedule(id);
            return PartialView("../Admin/Queue/_ScheduleEditor", ovm);
        }

        [HttpGet]
        //[AuthorizeUsers]
        public ActionResult AddSchedule()
        {
            var queueid = Convert.ToInt64(Request.QueryString["queueid"].ToString());
            QueueScheduleVM ovm = new QueueScheduleVM() { Id = 0, QueueId = queueid };
            return PartialView("../Admin/Queue/_ScheduleEditor", ovm);
        }

        // POST: Queue/DeleteSchedule/5     
        //[AuthorizeUsers]        
        [HttpPost, ActionName("DeleteSchedule")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteSchConfirmed(long id, long queueid)
        {
            new QueueService().DeleteQSchedule(id);

            var oQueue = new QueueService().GetQueue(queueid);
            return PartialView("../Admin/Queue/_QueueSchedules", oQueue.Schedules);

            //return Json(new { success = true, message = "Record deleted successfully" });
        }
        // POST: Queue/EditSchedule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[AuthorizeUsers] 
        public ActionResult EditSchedule(long id, QueueScheduleVM ovm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Set weeklySchedule from Formcollection
                    ovm.WeeklySchedule = Request.Form["WeeklySchedule"].ToString().Replace(",", "");

                    new QueueService().AddOrUpdateQSchedule(ovm);
                    // if no errors then refresh partialview
                    var oQueue = new QueueService().GetQueue(ovm.QueueId);
                    if (oQueue == null) return HttpNotFound();
                    return PartialView("../Admin/Queue/_QueueSchedules", oQueue.Schedules); //==> this works

                    //return Json(new { ok = true, html = PartialViewResult("_ScheduleEditor", ovm), message = "Data submitted successfully." });
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                    // If invalid, return the partial view again with error
                    //ViewBag.Error = ex.Message;
                    //return PartialView("../Admin/Queue/_ScheduleEditor", ovm);
                }
            }
            else
            {
                // If invalid, return the partial view again with validation errors
                //return PartialView("../Admin/Queue/_ScheduleEditor", ovm);
                //throw new Exception("Validation Error");

                Response.StatusCode = 400;
                //return Json(new { success = false, message = "Validation Failed", error = "Vaidation error" });

                return Json(new { ok = false, message = "Validation Failed", errors = ModelState.ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToList()) });

            }
        }

        #endregion Schedule actions


        #region Service actions: Add Edit Delete
        // GET: Queue/AddService
        [HttpGet]
        //[AuthorizeUsers]
        public ActionResult AddService()
        {
            var queueid = Convert.ToInt64(Request.QueryString["queueid"].ToString());
            QueueServiceVM ovm = new QueueServiceVM() { Id = 0, QueueId = queueid };
            return PartialView("../Admin/Queue/_ServiceEditor", ovm);
        }

        // GET: Queue/EditService(Insert)
        // GET: Queue/EditService/5(Update)
        [HttpGet]
        //[AuthorizeUsers] 
        public ActionResult EditService(long id = 0)
        {
            QueueServiceVM ovm = new QueueService().GetQueueService(id);
            return PartialView("../Admin/Queue/_ServiceEditor", ovm);
        }

        // POST: Queue/EditService/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[AuthorizeUsers] 
        //public ActionResult EditService(long id, FormCollection collection)
        public ActionResult EditService(long id, QueueServiceVM ovm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    new QueueService().AddOrUpdateQService(ovm);
                    // if no errors then refresh partialview
                    var oQueue = new QueueService().GetQueue(ovm.QueueId);

                    if (oQueue == null) return HttpNotFound();
                    return PartialView("../Admin/Queue/_QueueServices", oQueue.Services);
                    //return Json(new { success = true, html = View("_ServiceEditor", ovm), message = "Data submitted successfully." });
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
            }
            else
            {
                // If invalid, return the partial view again with validation errors

                Response.StatusCode = 400;
                //return PartialView("_ServiceEditor", ovm);
                return Json(new { success = false, message = "Validation Failed", errors = ModelState.ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToList()) });
            }
        }

        // POST: Queue/DeleteService/5     
        //[AuthorizeUsers]        
        [HttpPost, ActionName("DeleteService")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteSvcConfirmed(long id, long queueid)
        {
            new QueueService().DeleteQService(id);

            var oQueue = new QueueService().GetQueue(queueid);
            return PartialView("../Admin/Queue/_QueueServices", oQueue.Services);

            //return Json(new { success = true, message = "Record deleted successfully" });
        }
        #endregion
    }
}
