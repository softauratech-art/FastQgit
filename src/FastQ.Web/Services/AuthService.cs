using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Web;

namespace FastQ.Web.Services
{
    //public enum FQRole
    //{
    //    Host,       // Value 0 by default
    //    Provider,   // Value 1 by default
    //    QueueAdmin, // Value 2 by default
    //    Reporter,   // Value 3 by default
    //    SuperAdmin  // Value 4 by default
    //}
    public class AuthService
    {
        public string GetLoggedInWindowsUser()
        {
            var httpIdentityName = HttpContext.Current?.User?.Identity?.Name ?? string.Empty;
            httpIdentityName = ExtractAccountName(httpIdentityName);
            return httpIdentityName;
        }

        private static string ExtractAccountName(string identityName)
        {
            if (string.IsNullOrWhiteSpace(identityName))
            {
                return string.Empty;
            }

            var slashIndex = identityName.IndexOf("\\", StringComparison.Ordinal);
            if (slashIndex >= 0 && slashIndex < identityName.Length - 1)
            {
                return identityName.Substring(slashIndex + 1);
            }

            return identityName;
        }

        public int GetSessionEntityId()
        {
            var httpContext = HttpContext.Current;
            string seid = httpContext.Session?["fq_current_entity"]?.ToString();
            
            if (!string.IsNullOrWhiteSpace(seid) && Int32.TryParse(seid, out int entityid))
                return entityid;
            return 0;
        }
        public void SetSessionEntityId(string param)
        {
            if (!string.IsNullOrWhiteSpace(param) && Int32.TryParse(param, out int entityid))
            {
                HttpContext.Current.Session["fq_current_entity"] = entityid;
            }
            else
            {
                if (GetSessionEntityId() == 0)
                    HttpContext.Current.Session["fq_current_entity"] = 1;  //--default to 1 (Phase I for PEDS agency)
                //TODO: otherwise set it to oUser's 1st active BusinessEntities.entry                
            }
        }

        public bool IsInRole(FastQ.Web.Helpers.Utilities.FQRole role)
        {
            /* Inspect the Session-User-Object for roles and permissions
             *    TODO: WIP PR 3.9.2026           
             */

            bool result= false;
            var httpContext = HttpContext.Current;
            if (httpContext.Session?["fq_user"] == null)  return false;

            int eid = new AuthService().GetSessionEntityId();

            FastQ.Data.Entities.User ousr = (FastQ.Data.Entities.User)httpContext.Session["fq_user"];

            switch (role) {
                case Helpers.Utilities.FQRole.Host:
                    result = ousr.Queues.FirstOrDefault(l => l.HostFlag == true) != null && ousr.BusinessEntities.FirstOrDefault(e => e.EntityId == eid) != null;
                    break;
                case Helpers.Utilities.FQRole.Provider:
                    result = ousr.Queues.FirstOrDefault(l => l.ProviderFlag == true) != null && ousr.BusinessEntities.FirstOrDefault(e => e.EntityId == eid) != null;
                    break;
                case Helpers.Utilities.FQRole.QueueAdmin:
                    result = ousr.Queues.FirstOrDefault(l => l.QueueAdminFlag == true) != null && ousr.BusinessEntities.FirstOrDefault(e => e.EntityId == eid) != null;
                    break;
                case Helpers.Utilities.FQRole.Reporter:
                    result = ousr.Queues.FirstOrDefault(l => l.ReporterFlag == true) != null && ousr.BusinessEntities.FirstOrDefault(e => e.EntityId == eid) != null; 
                    break;
                case Helpers.Utilities.FQRole.SuperAdmin:                    
                    result = ousr.BusinessEntities.FirstOrDefault(e => e.ConfigAdminFlag == true && e.EntityId == eid) != null; 
                    break;                    
                default: return false;
            }
            return result;
        }

        public bool isAdminForQueue(long qid)
        {
            var httpContext = HttpContext.Current;
            if (httpContext.Session?["fq_user"] == null) return false;

            FastQ.Data.Entities.User ousr = (FastQ.Data.Entities.User)httpContext.Session["fq_user"];
            return (ousr.Queues.FirstOrDefault(l => l.QueueId == qid && l.QueueAdminFlag == true) != null);
        }

        #region OBSOLETE
        //public string GetLoggedInWindowsUser()
        //{
        //    var envUserName = string.Empty;
        //    var windowsIdentityName = string.Empty;
        //    var logonIdentityName = string.Empty;
        //    var httpIdentityName = string.Empty;
        //    var ntid = string.Empty;

        //    try
        //    {
        //        envUserName = Environment.UserName ?? string.Empty;
        //        windowsIdentityName = WindowsIdentity.GetCurrent()?.Name ?? string.Empty;
        //        logonIdentityName = HttpContext.Current?.Request?.LogonUserIdentity?.Name ?? string.Empty;
        //        httpIdentityName = HttpContext.Current?.User?.Identity?.Name ?? string.Empty;

        //        httpIdentityName = ExtractAccountName(httpIdentityName);
        //        logonIdentityName = ExtractAccountName(logonIdentityName);

        //        if (!string.IsNullOrWhiteSpace(windowsIdentityName) && !windowsIdentityName.Contains("IIS APPPOOL"))
        //        {
        //            ntid = envUserName;
        //        }
        //        else if (!string.IsNullOrWhiteSpace(logonIdentityName) && !logonIdentityName.Contains("NT AUTHORITY"))
        //        {
        //            ntid = logonIdentityName;
        //        }
        //        else
        //        {
        //            ntid = httpIdentityName;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.TraceError("AuthService.GetLoggedInWindowsUser failed: {0}", ex);
        //    }

        //    return ntid;
        //}
        #endregion

    }
}
