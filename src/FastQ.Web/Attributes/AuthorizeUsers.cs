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
        public bool IsAuthenticated = false;
        private AuthService authsvc = new AuthService();
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            string _stampuser = authsvc.GetLoggedInWindowsUser();
            bool result = false;

            // 1. Authenticate: Check Session_Object first. If null then query database for user-info, Else read from Session
            //    True if userid exists and isActive in FQ_USERS  table, OR if Session[usr] exists
            if (httpContext.Session?["fq_user"] == null || httpContext.Session?["fq_user"] is not User)
            {
                FastQ.Data.Repositories.IUserRepository _fastquser = DbRepositoryFactory.CreateUserRepository();
                var usr = _fastquser.Get(_stampuser, "AUTHSERVICE");

                if (usr == null || !usr.ActiveFlag || usr.BusinessEntities?.Count(e => e.ActiveFlag == true) == 0)
                {
                    IsAuthenticated = false;
                    return false;
                }

                // If user found in FQ-DB then store in Session
                // for further Role-Queue-Access processing handled by Controllers/AuthService
                httpContext.Session["fq_user"] = usr;

                // Store HttpCookie on client to show Entity-options at Session Timeout
                //  used when User is Active-user in multiple Entities
                var httpCookie = new HttpCookie("fq_user_entities", string.Join(",", usr.BusinessEntities.Where(e => e.ActiveFlag == true).Select(e => e.EntityId + ":" + e.EntityName).ToList()));
                httpContext.Response.Cookies.Add(httpCookie);
            }

            // Set isAuthenticated flag to be used in FilterContext Redirects for unauthorized access
            if (httpContext.Session?["fq_user"] != null && httpContext.Session?["fq_user"] is User)
            {
                result = true;
                IsAuthenticated = true;
                //Sets session_entity if missing
                authsvc.SetSessionEntityId();                
            }

            // 2. Authorize: If Roles sent as argument, then validate against User object
            if (IsAuthenticated && !string.IsNullOrWhiteSpace(AllowRole))
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
                    if (authsvc.IsInRole(role)) return true;
                }                
            }
            return result;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (IsAuthenticated)
            {
                string reason = ""; // (authsvc.GetSessionEntityId() == 0)? "Timeout": "AccessDenied";
                filterContext.Result = new RedirectToRouteResult(
                            new RouteValueDictionary
                            {
                                { "controller", "Home" },
                                { "action", "Restricted" },
                                { "path",  filterContext.HttpContext.Request.Path},
                                { "reason",  reason}
                            });
                //filterContext.Result = new RedirectResult("~/Home/Restricted");  //Controller-Restricted-View
            }
            else
                filterContext.Result = new RedirectResult("~/Unauthorized.aspx");
        }
    }
}