using System;
using System.Web.Script.Services;
using System.Web.Services;
using FastQ.Data.Common;
using FastQ.Web.App_Start;

namespace FastQ.Web.Provider
{
    public partial class Today : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e) { }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object GetQueueSnapshot(string locationId, string queueId)
        {
            if (!Guid.TryParse(locationId, out var locId) || !Guid.TryParse(queueId, out var qId))
                return new { ok = false, error = "locationId and queueId are required" };

            var dto = CompositionRoot.Queries.GetQueueSnapshot(locId, qId);
            return new { ok = true, data = dto };
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object ProviderAction(string action, string appointmentId, string providerId)
        {
            if (string.IsNullOrWhiteSpace(action) || !Guid.TryParse(appointmentId, out var apptId))
                return new { ok = false, error = "action and appointmentId are required" };

            action = action.Trim().ToLowerInvariant();
            if (!Guid.TryParse(providerId, out var provId))
                provId = Guid.Parse("02a62b1c-0000-0000-4641-535451494430");

            var res = action switch
            {
                "arrive" => CompositionRoot.Provider.MarkArrived(apptId),
                "begin" => CompositionRoot.Provider.BeginService(apptId, provId),
                "end" => CompositionRoot.Provider.EndService(apptId),
                _ => Result.Fail("Unknown action")
            };

            if (!res.Ok)
                return new { ok = false, error = res.Error };

            return new { ok = true };
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object TransferAppointment(string appointmentId, string targetQueueId)
        {
            if (!Guid.TryParse(appointmentId, out var apptId) || !Guid.TryParse(targetQueueId, out var queueId))
                return new { ok = false, error = "appointmentId and targetQueueId are required" };

            var res = CompositionRoot.Transfer.Transfer(apptId, queueId);
            if (!res.Ok)
                return new { ok = false, error = res.Error };

            return new
            {
                ok = true,
                newAppointmentId = res.Value.Id,
                newQueueId = res.Value.QueueId
            };
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object SystemClose(int staleHours)
        {
            var hours = staleHours <= 0 ? 12 : staleHours;
            var closed = CompositionRoot.SystemClose.CloseStaleScheduledAppointments(hours);
            return new { ok = true, closed = closed };
        }
    }
}

