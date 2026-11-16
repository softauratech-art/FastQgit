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
        private static readonly NLog.Logger Logger = NLog.LogManager.GetLogger("FastQ.UserManagement");
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
                if (oUser == null)
                {
                    ViewBag.ErrorReference = LogUserFailure("LoadUserNotFound", id, null);
                    ViewBag.ErrorTitle = "User not found";
                    ViewBag.ErrorMessage = "No user was returned for this user ID. Return to the user list and refresh it before trying again.";
                    ViewBag.BackToUsers = true;
                    Response.StatusCode = 404;
                    Response.TrySkipIisCustomErrors = true;
                    return View("Error");
                }
                return View("../Admin/User/ManageUser", oUser);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorReference = LogUserFailure("LoadUser", id, ex);
                var denied = ex is UnauthorizedAccessException;
                ViewBag.ErrorTitle = denied ? "Permission denied" : "Unable to open user settings";
                ViewBag.ErrorMessage = denied
                    ? "Your account could not access this user's details or permissions. Ask your administrator to check your access for the selected entity."
                    : "The user's details or queue permissions could not be loaded. This does not mean the user is missing. Return to the user list and try again. If the problem continues, ask your administrator to check access and the database lookup.";
                ViewBag.ErrorDetail = ex.Message;
                ViewBag.BackToUsers = true;
                Response.StatusCode = denied ? 403 : 500;
                Response.TrySkipIisCustomErrors = true;
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
            return Details(id);
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
                    ovm.LobbyQueues = collection["lobby"];
                    string hostqueues = collection["host"]?.ToString();
                    string providerqueues = collection["provider"]?.ToString();
                    string reporterqueues = collection["reporter"]?.ToString();
                    string adminqueues = collection["queueadmin"]?.ToString();
                    ovm.OtherLanguage = collection["language"]?.ToString();
                    
                    // Add to database
                    _service.AddOrUpdateUser(action, ovm, hostqueues, providerqueues, reporterqueues, adminqueues);

                    // Keep the submitted model if reloading fails after the save.
                    var savedUser = _service.GetUser(uid);
                    if (savedUser == null)
                        throw new InvalidOperationException("The user was saved, but could not be reloaded. Return to the user list and try again.");
                    ovm = savedUser;
                    ViewBag.SuccessMessage = "Record updated successfully";
                }
                else
                {
                    ovm.Permissions = ReloadPermissions(collection);
                    ovm.OtherLanguage = collection["language"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                var reference = LogUserFailure("SaveUser:" + action, ovm?.UserId, ex);
                ovm.Permissions = ReloadPermissions(collection);
                ovm.OtherLanguage = collection["language"]?.ToString();
                ViewBag.ErrorMessage = ex.Message + " Error reference: " + reference;
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
                return Json(new { response = "success", message = "Record deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { response = "error", message = ex.Message });
            }
        }

        private string LogUserFailure(string operation, string targetUserId, Exception exception)
        {
            var reference = Guid.NewGuid().ToString("N");
            // Encode request values so newlines cannot forge additional log entries.
            var context = new
            {
                ErrorReference = reference,
                Operation = operation,
                TargetUserId = targetUserId,
                Actor = User?.Identity?.Name,
                EntityId = Session?["fq_current_entity"]?.ToString(),
                Action = RouteData?.Values["action"]?.ToString()
            };
            var message = Newtonsoft.Json.JsonConvert.SerializeObject(context);
            if (exception == null) Logger.Warn("User management lookup returned no user. Context={0}", message);
            else Logger.Error(exception, "User management operation failed. Context={0}", message);
            return reference;
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
                string[] requiredperms = ["host", "queueadmin", "provider", "reporter", "lobby"];
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
            string[] queuepermsfld = ["host", "provider", "reporter", "queueadmin", "lobby"];
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
                        if (key.Equals("lobby")) perm.LobbyFlag = true;
                        if (key.Equals("queueadmin")) perm.QueueAdminFlag = true;
                        perms.Add(perm);
                    }
                    else
                    {
                        if (key.Equals("host")) foundItem.HostFlag = true;
                        if (key.Equals("provider")) foundItem.ProviderFlag = true;
                        if (key.Equals("reporter")) foundItem.ReporterFlag = true;
                        if (key.Equals("lobby")) foundItem.LobbyFlag = true;
                        if (key.Equals("queueadmin")) foundItem.QueueAdminFlag = true;
                    }
                }
            }
            return perms;
        }
    }
}
