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
    public class UserController : Controller
    {
        // GET: User       
        private readonly UserService _service;
        public UserController()
        {
            _service = new UserService();
        }
        
        public ActionResult Index()        
        {
            IList<UserVM> lUsers;
            try
            {                
                lUsers = _service.ListUsers();
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
                var oUser = _service.GetUser(id);
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
            var oUser = new UserVM { UserId = "", IsActive = true };
            return View("../Admin/User/ManageUser", oUser);
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(FormCollection collection, UserVM ovm)
        {
            return AddOrUpdateUser("A", collection, ovm);
        }

        // GET: User/Edit/ocuser01        
        public ActionResult Edit(string id)
        {                       
            var oUser = _service.GetUser(id);
            return View("../Admin/User/ManageUser", oUser);
        }

        // POST: User/Edit/ocuser01
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string id, FormCollection collection, UserVM ovm)
        {
            return AddOrUpdateUser("U", collection, ovm);
        }

        private ActionResult AddOrUpdateUser(string action, FormCollection collection, UserVM ovm)
        {
            try
            {
                if (IsValid(action, collection))
                {
                    string uid = ovm.UserId;
                    string hostqueues = collection["host"]?.ToString();
                    string providerqueues = collection["provider"]?.ToString();
                    string reporterqueues = collection["reporter"]?.ToString();
                    string adminqueues = collection["queueadmin"]?.ToString();
                    ovm.OtherLanguage = collection["language"]?.ToString();
                    
                    // Add to database
                    _service.AddOrUpdateUser(action, ovm, hostqueues, providerqueues, reporterqueues, adminqueues);

                    ViewBag.SuccessMessage = "Record updated successfully";

                    // Reload userinfo from database
                    ovm = _service.GetUser(uid);
                }
                else
                {
                    ovm.Permissions = ReloadPermissions(collection);
                    ovm.OtherLanguage = collection["language"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                ovm.Permissions = ReloadPermissions(collection);
                ovm.OtherLanguage = collection["language"]?.ToString();
                ViewBag.ErrorMessage = ex.Message;
            }

            return View("../Admin/User/ManageUser", ovm);            
        }

        // POST: Queue/DeleteSchedule/5
        [HttpPost]
        public JsonResult DeleteConfirmed(string uid)
        {
            try
            {
                _service.Delete(uid);
                ViewBag.SuccessMessage = $"User {uid} deleted successfully";
                //return Json("Record deleted successfully!");
                return Json(new { response = "success", message = "Record deleted successfully." });

            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                //return Json(ex.Message);
                return Json(new { response = "error", message = ex.Message });

            }

            //return RedirectToAction("../Admin/User");
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

        List<UserQueuePermission> ReloadPermissions(FormCollection fc)
        {   
            List<UserQueuePermission> perms = [];
            string[] queuepermsfld = ["host", "provider", "reporter", "queueadmin"];
            foreach (string key in queuepermsfld)
            {
                if (fc[key] == null) continue;
                string[] ids = fc[key]?.Split(',');
                foreach (string id in ids)
                {
                    long qid = long.Parse(id);
                    var foundItem = perms.FirstOrDefault(p => p.QueueId == qid);
                    if (foundItem == null)
                    {
                        var perm = new UserQueuePermission { QueueId = qid };
                        if (key.Equals("host")) perm.HostFlag = true;
                        if (key.Equals("provider")) perm.ProviderFlag = true;
                        if (key.Equals("reporter")) perm.ReporterFlag = true;
                        if (key.Equals("queueadmin")) perm.QueueAdminFlag = true;
                        perms.Add(perm);
                    }
                    else
                    {
                        if (key.Equals("host")) foundItem.HostFlag = true;
                        if (key.Equals("provider")) foundItem.ProviderFlag = true;
                        if (key.Equals("reporter")) foundItem.ReporterFlag = true;
                        if (key.Equals("queueadmin")) foundItem.QueueAdminFlag = true;
                    }
                }
            }
            return perms;
        }
    }
}
