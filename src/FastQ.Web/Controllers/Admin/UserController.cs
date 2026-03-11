using System;
using FastQ.Web.Attributes;
using FastQ.Web.Models.Admin;
using System.Collections.Generic;
using System.Web.Mvc;
using FastQ.Web.Services;

namespace FastQ.Web.Controllers.Admin
{
    [FQAuthorizeUser(AllowRole = "SuperAdmin")]
    public class UserController : Controller
    {        
        // GET: User       
             
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
        public ActionResult Create()
        {
            var oUser = new UserVM();
            return View("../Admin/User/ManageUser", oUser);
        }

        // POST: User/Create
        [HttpPost]        
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
        
        public ActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {               
                return View("../Admin/User/ManageUser", new UserVM { UserId = "", IsActive=true });
            }
                       
            var oUser = new UserService().GetUser(id);
            return View("../Admin/User/ManageUser", oUser);
        }

        // POST: User/Edit/ocuser01
        [HttpPost]        
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

        // GET: User/Delete/ocuser01        
        public ActionResult Delete(string id)
        {
            var oUser = new UserService().GetUser(id);
            return View("../Admin/User/ManageUser", oUser);
        }

        // POST: User/Delete/5
        [HttpPost]        
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
