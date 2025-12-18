using System.Web;
using FastQ.Web.App_Start;

namespace FastQ.Web.Api
{
    public class SystemCloseHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var raw = HandlerUtil.GetString(context.Request, "staleHours");
            int staleHours = 12;
            int.TryParse(raw, out staleHours);

            var closed = CompositionRoot.SystemClose.CloseStaleScheduledAppointments(staleHours);
            HandlerUtil.WriteJson(context, new { ok = true, closed = closed });
        }

        public bool IsReusable => true;
    }
}
