using System;
using System.Web;
using FastQ.Domain.Entities;
using FastQ.Web.App_Start;

namespace FastQ.Web.Api
{
    public class AdminUpdateHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var queueId = HandlerUtil.GetGuid(context.Request, "queueId");
            if (!queueId.HasValue)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = "queueId is required" }, 400);
                return;
            }

            if (!TryGetInt(context.Request, "maxUpcoming", out var maxUpcoming) ||
                !TryGetInt(context.Request, "maxDaysAhead", out var maxDaysAhead) ||
                !TryGetInt(context.Request, "minHoursLead", out var minHoursLead))
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = "Invalid configuration values" }, 400);
                return;
            }

            var queue = CompositionRoot.Queues.Get(queueId.Value);
            if (queue == null)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = "Queue not found" }, 404);
                return;
            }

            if (queue.Config == null)
                queue.Config = new QueueConfig();
            queue.Config.MaxUpcomingAppointments = maxUpcoming;
            queue.Config.MaxDaysAhead = maxDaysAhead;
            queue.Config.MinHoursLead = minHoursLead;

            CompositionRoot.Queues.Update(queue);

            HandlerUtil.WriteJson(context, new { ok = true });
        }

        private bool TryGetInt(HttpRequest request, string key, out int value)
        {
            var raw = request[key];
            return int.TryParse(raw, out value);
        }

        public bool IsReusable => true;
    }
}
