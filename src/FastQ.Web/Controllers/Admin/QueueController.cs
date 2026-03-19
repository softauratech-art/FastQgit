using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FastQ.Web.Attributes;
using FastQ.Web.Models.Admin;
using FastQ.Web.Services;
using FastQ.Web.Helpers;

namespace FastQ.Web.Controllers.Admin
{
    [FQAuthorizeUser(AllowRole = $"{nameof(Utilities.FQRole.QueueAdmin)},{nameof(Utilities.FQRole.SuperAdmin)}")]
    public class QueueController : Controller
    {
        private readonly QueueService _service;
        private readonly string controllerpath = "../Admin/Queue/";
        public QueueController()
        {
            _service = new QueueService();
        }

        #region Queue-Base-Record
        // GET: User       
        //[Route("admin")]
        public ActionResult Index()
        {
            IList<QueueVM> lQueues;
            try
            {
                lQueues = _service.ListQueues();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("Error");
            }

            return View(controllerpath + "List", lQueues);
        }             

        // GET: Queue/Edit(Insert)
        // GET: Queue/Edit/5(Update)
        public ActionResult Edit(long id = 0)
        {
            //If id=0 For-Insert
            if (id == 0)
                return View(controllerpath + "ManageQueue", new QueueVM { Id = 0, LocationId = 1 });

            //else For-Update
            var oQueue = _service.GetQueue(id);
            oQueue.LeadTimeMin = Helpers.Utilities.ParseDurationFromISO(oQueue.LeadTimeMin);
            oQueue.LeadTimeMax = Helpers.Utilities.ParseDurationFromISO(oQueue.LeadTimeMax);

            return View(controllerpath + "ManageQueue", oQueue);
        }

        //POST: Queue/Edit/10001
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(QueueVM ovm)
        {
            try
            {
                //set these from Form[] since we're using custom-checkbox in View-cshtml
                ovm.ActiveFlag = Request.Form["ActiveFlag"] != null ? Request.Form["ActiveFlag"].Equals("true") : false;
                ovm.EmpOnly = Request.Form["EmpOnly"] != null ? Request.Form["EmpOnly"].Equals("true") : false;
                ovm.HideInKiosk = Request.Form["HideInKiosk"] != null ? Request.Form["HideInKiosk"].Equals("true") : false;
                ovm.HideInMonitor = Request.Form["HideInMonitor"] != null ? Request.Form["HideInMonitor"].Equals("true") : false;
                ovm.HasGuidelines = Request.Form["HasGuidelines"] != null ? Request.Form["HasGuidelines"].Equals("true") : false;
                ovm.HasUploads = Request.Form["HasUploads"] != null ? Request.Form["HasUploads"].Equals("true") : false;

                if (ModelState.IsValid)
                {
                    Int64 id = _service.AddOrUpdateQueue(ovm);
                    ViewBag.SuccessMessage = "Data saved successfully";

                    // if no errors then send to Details-view
                    var oQueue = _service.GetQueue(id);
                    if (oQueue == null) return HttpNotFound();
                    
                    if (ovm.Id == 0)
                        return RedirectToAction("Edit", "Queue", new { id = oQueue.Id });
                    else
                        return View(controllerpath + "ManageQueue", oQueue);                    
                }

                ViewBag.ErrorMessage = "Validation failed. Please check the details.";
                return View(controllerpath + "ManageQueue", ovm);
            }
            catch (Exception ex) 
            {
                ViewBag.ErrorMessage = "An error occurred while processing your request. " + ex.Message;
                return View(controllerpath + "ManageQueue", ovm);
            }
        }

        // GET: Queue/Delete/10001
        public ActionResult Delete(Int64 id)
        {
            try
            {
                _service.Delete(id);
                ViewBag.SuccessMessage = "Record deleted successfully";
            }
            catch (Exception ex){
                ViewBag.ErrorMessage = "An error occurred while processing your request. " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        #endregion Queue-Base-Record

        #region Schedule actions
        // GET: Queue/EditSchedule(Insert)
        // GET: Queue/EditSchedule/5(Update)
        [HttpGet]
        public ActionResult EditSchedule(long id = 0)
        {
            if (id == 0)
            {
                var queueid = Convert.ToInt64(Request.QueryString["queueid"].ToString());
                return PartialView(controllerpath + "_ScheduleEditor", new QueueScheduleVM() { Id = 0, QueueId = queueid });
            }

            QueueScheduleVM ovm = _service.GetQueueSchedule(id);
            return PartialView(controllerpath + "_ScheduleEditor", ovm);
        }

        // POST: Queue/DeleteSchedule/5
        [HttpPost, ActionName("DeleteSchedule")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteSchConfirmed(long id, long queueid)
        {
            _service.DeleteQSchedule(id);

            var oQueue = _service.GetQueue(queueid);
            return PartialView(controllerpath + "_QueueSchedules", oQueue.Schedules);

            //return Json(new { success = true, message = "Record deleted successfully" });
        }

        // POST: Queue/EditSchedule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSchedule(long id, QueueScheduleVM ovm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Set weeklySchedule from Formcollection
                    ovm.WeeklySchedule = Request.Form["WeeklySchedule"].ToString().Replace(",", "");

                    _service.AddOrUpdateQSchedule(ovm);
                    // if no errors then refresh partialview
                    var oQueue = _service.GetQueue(ovm.QueueId);
                    if (oQueue == null) return HttpNotFound();
                    return PartialView(controllerpath + "_QueueSchedules", oQueue.Schedules); //==> this works

                    //return Json(new { ok = true, html = PartialViewResult("_ScheduleEditor", ovm), message = "Data submitted successfully." });
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                    // If invalid, return the partial view again with error
                    //ViewBag.Error = ex.Message;
                    //return PartialView(controllerpath + "_ScheduleEditor", ovm);
                }
            }
            else
            {
                // If invalid, return the partial view again with validation errors
                //return PartialView(controllerpath + "_ScheduleEditor", ovm);
                //throw new Exception("Validation Error");

                Response.StatusCode = 400;
                //return Json(new { success = false, message = "Validation Failed", error = "Vaidation error" });

                return Json(new { ok = false, message = "Validation Failed", errors = ModelState.ToDictionary(k => k.Key, v => v.Value.Errors.Select(e => e.ErrorMessage).ToList()) });

            }
        }

        #endregion Schedule actions


        #region Service actions: Add Edit Delete

        [HttpGet]
        public ActionResult EditService(long id = 0)
        {            
            if (id == 0)
            {
                var queueid = Convert.ToInt64(Request.QueryString["queueid"].ToString()); 
                return PartialView(controllerpath + "_ServiceEditor", new QueueServiceVM() { Id = 0, QueueId = queueid });
            }
            QueueServiceVM ovm = _service.GetQueueService(id);
            return PartialView(controllerpath + "_ServiceEditor", ovm);
        }

        // POST: Queue/EditService/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditService(long id, QueueServiceVM ovm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _service.AddOrUpdateQService(ovm);
                    // if no errors then refresh partialview
                    var oQueue = _service.GetQueue(ovm.QueueId);

                    if (oQueue == null) return HttpNotFound();
                    return PartialView(controllerpath + "_QueueServices", oQueue.Services);
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
        [HttpPost, ActionName("DeleteService")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteSvcConfirmed(long id, long queueid)
        {
            _service.DeleteQService(id);

            var oQueue = _service.GetQueue(queueid);
            return PartialView(controllerpath + "_QueueServices", oQueue.Services);

            //return Json(new { success = true, message = "Record deleted successfully" });
        }
        #endregion
    }
}
