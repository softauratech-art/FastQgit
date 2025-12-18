using System;
using System.Web;
using FastQ.Web.App_Start;

namespace FastQ.Web.Api
{
    public class ProviderActionHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var action = HandlerUtil.GetString(context.Request, "action");
            var apptId = HandlerUtil.GetGuid(context.Request, "appointmentId");

            if (string.IsNullOrWhiteSpace(action) || apptId == null)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = "action and appointmentId are required" }, 400);
                return;
            }

            action = action.Trim().ToLowerInvariant();
            var providerIdRaw = HandlerUtil.GetString(context.Request, "providerId");
            Guid providerId;
            if (!Guid.TryParse(providerIdRaw, out providerId))
                providerId = Guid.Parse("44444444-4444-4444-4444-444444444444"); // demo provider

            var res = action switch
            {
                "arrive" => CompositionRoot.Provider.MarkArrived(apptId.Value),
                "begin" => CompositionRoot.Provider.BeginService(apptId.Value, providerId),
                "end" => CompositionRoot.Provider.EndService(apptId.Value),
                _ => FastQ.Domain.Common.Result.Fail("Unknown action")
            };

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
