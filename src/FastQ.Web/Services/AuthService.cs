using FastQ.Data.Db;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Web.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Web;

namespace FastQ.Web.Services
{
    public class AuthService
    {
        private readonly IUserRepository _users;

        public AuthService()
        {
            _users = DbRepositoryFactory.CreateUserRepository();
        }

        public string GetLoggedInWindowsUser()
        {
            string debugkey = ConfigurationManager.AppSettings["debugkey"]?.ToString();

            HttpContext httpContext = HttpContext.Current;            
            if (httpContext.Request["debug"] != null && httpContext.Request["debug"].ToString() == debugkey)
                 httpContext.Session["debug"] = httpContext.Request["debug"];

            bool isDebug = (httpContext.Session["debug"] != null && httpContext.Session["debug"].ToString() == debugkey);
            
            if (isDebug)
            {                
                //check if impersonation userid is in Session
                var impersonatedUserId = httpContext.Session?["debug_impersonateuserid"]?.ToString();
                if (impersonatedUserId != null) { return impersonatedUserId; }
                //otherwise read from querystring
                impersonatedUserId = httpContext.Request["debuguserid"]?.ToString();
                HttpContext.Current.Session["debug_impersonateuserid"] = impersonatedUserId;  
                return impersonatedUserId;
            }

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

        public long GetSessionEntityId()
        {
            var httpContext = HttpContext.Current;
            string seid = httpContext.Session?["fq_current_entity"]?.ToString();
            
            if (!string.IsNullOrWhiteSpace(seid) && Int32.TryParse(seid, out int entityid))
                return entityid;
            return 0;
        }
        public void SetSessionEntityId()
        {
            string param = HttpContext.Current.Request["eid"] != null ? HttpContext.Current.Request["eid"].ToString(): string.Empty;
            if (!string.IsNullOrWhiteSpace(param) && Int32.TryParse(param, out int entityid))
            {
                HttpContext.Current.Session["fq_current_entity"] = entityid;
            }
            else
            {
                long? eid = GetSessionEntityId();
                if (eid == 0)
                {
                    if (HttpContext.Current.Session?["fq_user"] != null && HttpContext.Current.Session?["fq_user"] is Data.Entities.User)
                    {
                        FastQ.Data.Entities.User ousr = (FastQ.Data.Entities.User)HttpContext.Current.Session["fq_user"];

                        int cnt = ousr.BusinessEntities.Count(e => e.ActiveFlag == true);                        
                        if (cnt == 1)
                            eid = ousr.BusinessEntities?.FirstOrDefault(e => e.ActiveFlag == true).EntityId;  //auto-default
                    }                    
                }
                HttpContext.Current.Session["fq_current_entity"] = eid;               
            }
        }

        public bool IsInRole(Helpers.Utilities.FQRole role)
        {
            /* Inspect the Session-User-Object for roles and permissions */

            bool result= false;
            FastQ.Data.Entities.User ousr = GetCurrentUser();
            long eid = new AuthService().GetSessionEntityId();            

            // Allow only if User has active access to This entity
            if (ousr.BusinessEntities?.FirstOrDefault(e => e.EntityId == eid && e.ActiveFlag == true) == null)
                return false;

            // Process Roles for User with active access to This entity
            switch (role) {
                case Helpers.Utilities.FQRole.Host:
                    result = ousr.Queues.FirstOrDefault(l => l.HostFlag == true && l.EntityId == eid) != null;
                    break;
                case Helpers.Utilities.FQRole.Provider:
                    result = ousr.Queues.FirstOrDefault(l => l.ProviderFlag == true && l.EntityId == eid) != null;
                    break;
                case Helpers.Utilities.FQRole.QueueAdmin:
                    result = ousr.Queues.FirstOrDefault(l => l.QueueAdminFlag == true && l.EntityId == eid) != null;
                    break;
                case Helpers.Utilities.FQRole.Reporter:
                    result = ousr.Queues.FirstOrDefault(l => l.ReporterFlag == true && l.EntityId == eid) != null; 
                    break;
                case Helpers.Utilities.FQRole.SuperAdmin:                    
                    result = ousr.BusinessEntities.FirstOrDefault(e => e.ConfigAdminFlag == true && e.EntityId == eid) != null; 
                    break;                    
                default: return false;
            }
            return result;
        }

        public bool IsAdminForQueue(long qid)
        {
            FastQ.Data.Entities.User ousr = GetCurrentUser();
            return (ousr.Queues.FirstOrDefault(l => l.QueueId == qid && l.QueueAdminFlag == true) != null);
        }

        public ServicePageAccess GetServicePageAccess()
        {
            var user = GetCurrentUser();
            var access = new ServicePageAccess();
            if (user == null)
            {
                return access;
            }

            var currentEntityId = GetSessionEntityId();
            var queuePermissions = (user.Queues ?? new List<UserQueuePermission>())
                .Where(q => q.QueueActiveFlag && (currentEntityId <= 0 || q.EntityId == currentEntityId))
                .ToList();
            var actionQueuePermissions = (_users.GetActionQueuePermissions(user.UserId) ?? new List<UserQueuePermission>())
                .Where(q => q.QueueActiveFlag && (currentEntityId <= 0 || q.EntityId == currentEntityId))
                .ToList();

            var isSuperAdmin = (user.BusinessEntities ?? new List<UserEntity>())
                .Any(e => e.ActiveFlag && e.ConfigAdminFlag && (currentEntityId <= 0 || e.EntityId == currentEntityId));

            access.IsAdmin = isSuperAdmin;
            access.HostQueueIds = actionQueuePermissions
                .Where(q => q.HostFlag)
                .Select(q => q.QueueId)
                .Distinct()
                .ToList();
            access.ProviderQueueIds = actionQueuePermissions
                .Where(q => q.ProviderFlag)
                .Select(q => q.QueueId)
                .Distinct()
                .ToList();
            access.QueueAdminQueueIds = actionQueuePermissions
                .Where(q => q.QueueAdminFlag)
                .Select(q => q.QueueId)
                .Distinct()
                .ToList();
            access.CanAddEntries = access.IsAdmin
                || access.ProviderQueueIds.Count > 0
                || access.QueueAdminQueueIds.Count > 0
                || access.HostQueueIds.Count > 0;

            return access;
        }

        public bool CanAccessQueueActions(long queueId)
        {
            var access = GetServicePageAccess();
            return HasQueueActionAccess(access, queueId);
        }

        public bool CanAccessProviderServiceActions(long queueId)
        {
            var access = GetServicePageAccess();
            return access != null
                   && (access.IsAdmin
                       || access.QueueAdminQueueIds.Contains(queueId)
                       || access.ProviderQueueIds.Contains(queueId));
        }

        public bool CanAddEntries(long queueId)
        {
            var access = GetServicePageAccess();
            return HasQueueActionAccess(access, queueId);
        }

        public User GetCurrentUser()
        {
            var httpContext = HttpContext.Current;
            if (httpContext?.Session?["fq_user"] is User user)
            {
                return user;
            }

            return null;
        }

        private static bool HasQueueActionAccess(ServicePageAccess access, long queueId)
        {
            if (access == null)
            {
                return false;
            }

            return access.IsAdmin
                   || access.QueueAdminQueueIds.Contains(queueId)
                   || access.ProviderQueueIds.Contains(queueId) 
                   || access.HostQueueIds.Contains(queueId);
        }
    }
}
