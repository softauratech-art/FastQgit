using System.Web;
using FastQ.Web.App_Start;

namespace FastQ.Web.Api
{
    public class AppointmentSnapshotHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var apptId = HandlerUtil.GetGuid(context.Request, "appointmentId");
            if (apptId == null)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = "appointmentId is required" }, 400);
                return;
            }

            var dto = CompositionRoot.Queries.GetAppointmentSnapshot(apptId.Value);
            if (dto == null)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = "appointment not found" }, 404);
                return;
            }

            HandlerUtil.WriteJson(context, new { ok = true, data = dto });
        }

        public bool IsReusable => true;
    }
}
