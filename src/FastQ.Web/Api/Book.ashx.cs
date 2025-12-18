using System.Web;
using FastQ.Web.App_Start;

namespace FastQ.Web.Api
{
    public class BookHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var locId = HandlerUtil.GetGuid(context.Request, "locationId");
            var queueId = HandlerUtil.GetGuid(context.Request, "queueId");
            var phone = HandlerUtil.GetString(context.Request, "phone");
            var name = HandlerUtil.GetString(context.Request, "name");
            var smsOptIn = HandlerUtil.GetString(context.Request, "smsOptIn") == "true";

            if (locId == null || queueId == null)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = "locationId and queueId are required" }, 400);
                return;
            }

            var res = CompositionRoot.Booking.BookFirstAvailable(locId.Value, queueId.Value, phone, smsOptIn, name);
            if (!res.Ok)
            {
                HandlerUtil.WriteJson(context, new { ok = false, error = res.Error }, 400);
                return;
            }

            HandlerUtil.WriteJson(context, new
            {
                ok = true,
                appointmentId = res.Value.Id,
                locationId = res.Value.LocationId,
                queueId = res.Value.QueueId,
                status = res.Value.Status.ToString()
            });
        }

        public bool IsReusable => true;
    }
}
