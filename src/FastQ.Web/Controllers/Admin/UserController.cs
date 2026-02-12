using System;
using FastQ.Web.Attributes;
using FastQ.Web.Models.Admin;
using System.Collections.Generic;
using System.Web.Mvc;
using FastQ.Web.Services;

namespace FastQ.Web.Controllers.Admin
{     
    public class UserController : Controller
    {        
        // GET: User       
        [AuthorizeUsers]       
        public ActionResult Index()        
        {
            IList<UserVM> lUsers;
            try
            {
                lUsers = new UserService().ListUsers();
            }
            catch (Exception ex) 
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("Error");
            }

            return View("../Admin/User/List", lUsers);
        }

        // GET: User/Details/ocuser01
        [AuthorizeUsers]
        public ActionResult Details(string id)
        {
            try 
            { 
                var oUser = new UserService().GetUser(id);
                return View("../Admin/User/ManageUser", oUser);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "An error occurred: " + ex.Message;
                return View("Error");
            }            
        }

        // GET: User/Create
        [AuthorizeUsers]
        public ActionResult Create()
        {
            var oUser = new UserVM();
            return View("../Admin/User/ManageUser", oUser);
        }

        // POST: User/Create
        [HttpPost]
        [AuthorizeUsers]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("../Admin/User/Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: User/Edit/ocuser01
        [AuthorizeUsers]
        public ActionResult Edit(string id)
        {
            var oUser = new UserService().GetUser(id);
            return View("../Admin/User/ManageUser", oUser);
        }

        // POST: User/Edit/ocuser01
        [HttpPost]
        [AuthorizeUsers]
        public ActionResult Edit(string id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("../Admin/User/Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: User/Delete/ocuser01
        [AuthorizeUsers]
        public ActionResult Delete(string id)
        {
            var oUser = new UserService().GetUser(id);
            return View("../Admin/User/ManageUser", oUser);
        }

        // POST: User/Delete/5
        [HttpPost]
        [AuthorizeUsers]
        public ActionResult Delete(string id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here
                
                return RedirectToAction("../Admin/User/Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
