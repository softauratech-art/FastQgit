using System;
using System.Web;
using FastQ.Web.App_Start;

namespace FastQ.Web.Api
{
    public class QueueSnapshotHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var locId = HandlerUtil.GetGuid(context.Request, "locationId");
            var queueId = HandlerUtil.GetGuid(context.Request, "queueId");

            if (locId == null || queueId == null)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = "locationId and queueId are required" }, 400);
                return;
            }

            var dto = CompositionRoot.Queries.GetQueueSnapshot(locId.Value, queueId.Value);
            HandlerUtil.WriteJson(context, new { ok = true, data = dto });
        }

        public bool IsReusable => true;
    }
}
