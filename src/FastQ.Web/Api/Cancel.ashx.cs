using System.Web;
using FastQ.Web.App_Start;

namespace FastQ.Web.Api
{
    public class CancelHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var apptId = HandlerUtil.GetGuid(context.Request, "appointmentId");
            if (apptId == null)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = "appointmentId is required" }, 400);
                return;
            }

            var res = CompositionRoot.Booking.Cancel(apptId.Value);
            if (!res.Ok)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = res.Error }, 400);
                return;
            }

            HandlerUtil.WriteJson(context, new { ok = true });
        }

        public bool IsReusable => true;
    }
}
