using FastQ.Web.Attributes;
using FastQ.Web.Helpers;
using FastQ.Web.Services;
using Microsoft.Ajax.Utilities;
using System;
using System.Web.Mvc;

namespace FastQ.Web.Controllers
{
    [FQAuthorizeUser]
    public class HomeController : Controller
    {
        //public const string allowedRoles = $"{nameof(Utilities.FQRole.Host)},{nameof(Utilities.FQRole.Provider)},{nameof(Utilities.FQRole.QueueAdmin)},{nameof(Utilities.FQRole.SuperAdmin)},{nameof(Utilities.FQRole.Reporter)}";
        //[FQAuthorizeUser(AllowRole = allowedRoles)]
        [HttpGet]
        public ActionResult Index(string eid)
        {
            new Services.AuthService().SetSessionEntityId(eid);
            return View();
        }
        
        public ActionResult Restricted(string path)
        {            
            ViewBag.errmsg = path;
            return View();
        }
    }
}
