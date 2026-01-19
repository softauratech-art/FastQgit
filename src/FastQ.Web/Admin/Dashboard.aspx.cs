using System;
using System.Linq;
using System.Web.Script.Services;
using System.Web.Services;
using FastQ.Data.Entities;
using FastQ.Web.App_Start;

namespace FastQ.Web.Admin
{
    public partial class Dashboard : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e) { }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object AdminSnapshot(string locationId)
        {
            Guid locId;
            var hasLocation = Guid.TryParse(locationId, out locId);

            var queues = hasLocation
                ? CompositionRoot.Queues.ListByLocation(locId)
                : CompositionRoot.Queues.ListAll();

            var providers = hasLocation
                ? CompositionRoot.Providers.ListByLocation(locId)
                : CompositionRoot.Providers.ListAll();

            var locations = CompositionRoot.Locations.ListAll();

            var queueRows = queues.Select(q => new
            {
                QueueId = q.Id,
                QueueName = q.Name,
                MaxUpcomingAppointments = q.Config?.MaxUpcomingAppointments ?? 0,
                MaxDaysAhead = q.Config?.MaxDaysAhead ?? 0,
                MinHoursLead = q.Config?.MinHoursLead ?? 0
            }).ToList();

            var providerRows = providers.Select(p =>
            {
                var locationName = locations.FirstOrDefault(l => l.Id == p.LocationId)?.Name ?? "Unknown";
                return new
                {
                    ProviderId = p.Id,
                    ProviderName = p.Name,
                    LocationName = locationName
                };
            }).ToList();

            return new
            {
                ok = true,
                data = new
                {
                    Queues = queueRows,
                    Providers = providerRows
                }
            };
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public static object AdminUpdate(string queueId, string maxUpcoming, string maxDaysAhead, string minHoursLead)
        {
            if (!Guid.TryParse(queueId, out var qId))
                return new { ok = false, error = "queueId is required" };

            if (!int.TryParse(maxUpcoming, out var maxUpcomingInt) ||
                !int.TryParse(maxDaysAhead, out var maxDaysAheadInt) ||
                !int.TryParse(minHoursLead, out var minHoursLeadInt))
                return new { ok = false, error = "Invalid configuration values" };

            var queue = CompositionRoot.Queues.Get(qId);
            if (queue == null)
                return new { ok = false, error = "Queue not found" };

            if (queue.Config == null)
                queue.Config = new QueueConfig();

            queue.Config.MaxUpcomingAppointments = maxUpcomingInt;
            queue.Config.MaxDaysAhead = maxDaysAheadInt;
            queue.Config.MinHoursLead = minHoursLeadInt;

            CompositionRoot.Queues.Update(queue);

            return new { ok = true };
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
