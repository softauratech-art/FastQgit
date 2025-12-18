using System.Web;
using FastQ.Web.App_Start;

namespace FastQ.Web.Api
{
    public class TransferHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var apptId = HandlerUtil.GetGuid(context.Request, "appointmentId");
            var targetQueueId = HandlerUtil.GetGuid(context.Request, "targetQueueId");

            if (apptId == null || targetQueueId == null)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = "appointmentId and targetQueueId are required" }, 400);
                return;
            }

            var res = CompositionRoot.Transfer.Transfer(apptId.Value, targetQueueId.Value);
            if (!res.Ok)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = res.Error }, 400);
                return;
            }

            HandlerUtil.WriteJson(context, new
            {
                ok = true,
                newAppointmentId = res.Value.Id,
                newQueueId = res.Value.QueueId
            });
        }

        public bool IsReusable => true;
    }
}
