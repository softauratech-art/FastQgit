using System;
using FastQ.Web.Attributes;
using FastQ.Web.Models.Admin;
using System.Collections.Generic;
using System.Web.Mvc;
using FastQ.Web.Services;

namespace FastQ.Web.Controllers.Admin
{     
    public class QueueController : Controller
    {
        // GET: User       
        ////[AuthorizeUsers]        
        [Route("admin")]
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
        [Route("admin")]
        public ActionResult Details(Int64 id)
        {
            try 
            { 
                var oQueue = new QueueService().GetQueue(10003);
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
            var oQueue = new QueueService();
            return View("../Admin/Queue/View", oQueue);
        }

        // POST: Queue/Create
        [HttpPost]
        //[AuthorizeUsers] 
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Queue/Edit/10001
        [AuthorizeUsers] 
        public ActionResult Edit(string id)
        {
            var oQueue = new QueueService().GetQueue(10003);
            return View("../Admin/Queue/ManageQueue", oQueue);
        }

        // POST: Queue/Edit/10001
        [HttpPost]
        //[AuthorizeUsers] 
        public ActionResult Edit(string id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Queue/Delete/10001
        //[AuthorizeUsers] 
        public ActionResult Delete(Int64 id)
        {
            var oQueue = new QueueService().GetQueue(id);
            return View("ManageUser", oQueue);
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

        [HttpGet]
        public ActionResult DisplayNewSchedule()
        {
            return PartialView("../Admin/Queue/_ScheduleEdit", new QueueScheduleVM() { Id = 0 });
        }

        public ActionResult EditSchedule(long id)
        {
            return PartialView("../Admin/Queue/_ScheduleEdit", new QueueScheduleVM() { Id=id});
        }
    }
}
