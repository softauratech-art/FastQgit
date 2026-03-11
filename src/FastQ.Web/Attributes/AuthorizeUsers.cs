using FastQ.Data.Db;
using FastQ.Data.Entities;
using FastQ.Web.Services;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace FastQ.Web.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class FQAuthorizeUserAttribute : AuthorizeAttribute
    {
        public string AllowRole { get; set; }
        public bool isAuthenticated = false;

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            AuthService authService = new AuthService();
            string _stampuser = authService.GetLoggedInWindowsUser();
            bool result = false;

            // 1. Authenticate: Check Session_Object first. If null then query database for user-info, Else read from Session
            //    True if userid exists and isActive in FQ_USERS  table, OR if Session[usr] exists
            if (httpContext.Session?["fq_user"] == null || httpContext.Session?["fq_user"] is not User)
            {
                FastQ.Data.Repositories.IUserRepository _fastquser = DbRepositoryFactory.CreateUserRepository();
                var usr = _fastquser.Get(_stampuser, "AUTHSERVICE");

                if (usr == null || !usr.ActiveFlag)
                    return false;

                // If user found in FQ-DB then store in Session
                // for further Role-Queue-Access processing handled by Controllers/AuthService
                httpContext.Session["fq_user"] = usr;
                httpContext.Session["fq_user_entityid"] = (usr.BusinessEntities?.OrderBy(l => l.EntityId).FirstOrDefault(l => l.ActiveFlag == true).EntityId);
            }

            // Set isAuthenticated flag to be used in FilterContext Redirects for unauthorized access
            if (httpContext.Session?["fq_user"] != null && httpContext.Session?["fq_user"] is User)
            {
                result = true;
                isAuthenticated = true;
            }

            // 2. Authorize: If Roles sent as argument, then validate against User object
            if (isAuthenticated && !string.IsNullOrWhiteSpace(AllowRole))
            {
                //reset flag
                result = false;
                string[] roles = AllowRole.Split(',');
                Helpers.Utilities.FQRole role = new Helpers.Utilities.FQRole();
                foreach (var r in roles)
                {
                    if (r.Equals("Host")) role = Helpers.Utilities.FQRole.Host;
                    if (r.Equals("Provider")) role = Helpers.Utilities.FQRole.Provider;
                    if (r.Equals("QueueAdmin")) role = Helpers.Utilities.FQRole.QueueAdmin;
                    if (r.Equals("Reporter")) role = Helpers.Utilities.FQRole.Reporter;
                    if (r.Equals("SuperAdmin")) role = Helpers.Utilities.FQRole.SuperAdmin;
                    // If any Roles match, then return True and exit
                    if (authService.IsInRole(role)) return true;
                }                
            }
            return result;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (isAuthenticated)
            {               
                //filterContext.Result = new RedirectResult("~/Home/Restricted");  //Controller-Restricted-View
                filterContext.Result = new RedirectToRouteResult(
                            new RouteValueDictionary
                            {
                                { "controller", "Home" },
                                { "action", "Restricted" },
                                { "path",  filterContext.HttpContext.Request.Path}
                            });
            }
            else
                filterContext.Result = new RedirectResult("~/Unauthorized.aspx");
        }
    }
}