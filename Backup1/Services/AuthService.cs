using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Web;

namespace FastQ.Web.Services
{
    public class AuthService
    {
        public string GetLoggedInWindowsUser()
        {
            var envUserName = string.Empty;
            var windowsIdentityName = string.Empty;
            var logonIdentityName = string.Empty;
            var httpIdentityName = string.Empty;
            var ntid = string.Empty;

            try
            {
                envUserName = Environment.UserName ?? string.Empty;
                windowsIdentityName = WindowsIdentity.GetCurrent()?.Name ?? string.Empty;
                logonIdentityName = HttpContext.Current?.Request?.LogonUserIdentity?.Name ?? string.Empty;
                httpIdentityName = HttpContext.Current?.User?.Identity?.Name ?? string.Empty;

                httpIdentityName = ExtractAccountName(httpIdentityName);
                logonIdentityName = ExtractAccountName(logonIdentityName);

                if (!string.IsNullOrWhiteSpace(windowsIdentityName) && !windowsIdentityName.Contains("IIS APPPOOL"))
                {
                    ntid = envUserName;
                }
                else if (!string.IsNullOrWhiteSpace(logonIdentityName) && !logonIdentityName.Contains("NT AUTHORITY"))
                {
                    ntid = logonIdentityName;
                }
                else
                {
                    ntid = httpIdentityName;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("AuthService.GetLoggedInWindowsUser failed: {0}", ex);
            }

            return ntid;
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
    }
}
