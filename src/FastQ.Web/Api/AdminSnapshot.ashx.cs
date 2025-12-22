using System.Linq;
using System.Web;
using FastQ.Web.App_Start;

namespace FastQ.Web.Api
{
    public class AdminSnapshotHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var locationId = HandlerUtil.GetGuid(context.Request, "locationId");

            var queues = locationId.HasValue
                ? CompositionRoot.Queues.ListByLocation(locationId.Value)
                : CompositionRoot.Queues.ListAll();

            var providers = locationId.HasValue
                ? CompositionRoot.Providers.ListByLocation(locationId.Value)
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

            HandlerUtil.WriteJson(context, new
            {
                ok = true,
                data = new
                {
                    Queues = queueRows,
                    Providers = providerRows
                }
            });
        }

        public bool IsReusable => true;
    }
}
