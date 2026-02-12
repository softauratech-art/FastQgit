using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FastQ.Web.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AuthorizeUsersAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            return true;
            
            var users = ConfigurationManager.AppSettings["AuthorizedUsers"].Split(',');

            var user = users.FirstOrDefault(u => u.ToUpper() == httpContext.User.Identity.Name.ToUpper());
            if (user != null) return true;
                        
            return false;

            //TODO:
            // Call DB.proc to return Bool based on FQ_USERS table and IsActive column
            //
            //
            

        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("~/Home/Unauthorized");
        }
    }
   
}