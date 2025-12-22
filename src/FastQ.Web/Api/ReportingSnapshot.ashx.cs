using System;
using System.Linq;
using System.Web;
using FastQ.Domain.Entities;
using FastQ.Web.App_Start;

namespace FastQ.Web.Api
{
    public class ReportingSnapshotHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var locationId = HandlerUtil.GetGuid(context.Request, "locationId");
            var queueId = HandlerUtil.GetGuid(context.Request, "queueId");

            var appointments = locationId.HasValue
                ? CompositionRoot.Appointments.ListByLocation(locationId.Value).ToList()
                : CompositionRoot.Appointments.ListAll().ToList();

            if (queueId.HasValue)
                appointments = appointments.Where(a => a.QueueId == queueId.Value).ToList();

            var now = DateTime.UtcNow;
            var dayStart = now.Date;
            var dayEnd = dayStart.AddDays(1);

            var bookedToday = appointments.Count(a => a.CreatedUtc >= dayStart && a.CreatedUtc < dayEnd);
            var scheduledToday = appointments.Count(a => a.ScheduledForUtc >= dayStart && a.ScheduledForUtc < dayEnd);
            var completedToday = appointments.Count(a => a.UpdatedUtc >= dayStart && a.UpdatedUtc < dayEnd && a.Status == AppointmentStatus.Completed);
            var cancelledToday = appointments.Count(a => a.UpdatedUtc >= dayStart && a.UpdatedUtc < dayEnd &&
                                                       (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.ClosedBySystem));

            var providers = locationId.HasValue
                ? CompositionRoot.Providers.ListByLocation(locationId.Value)
                : CompositionRoot.Providers.ListAll();

            var providerRows = providers.Select(p => new
            {
                ProviderId = p.Id,
                ProviderName = p.Name,
                Arrived = appointments.Count(a => a.ProviderId == p.Id && a.Status == AppointmentStatus.Arrived),
                InService = appointments.Count(a => a.ProviderId == p.Id && a.Status == AppointmentStatus.InService),
                Completed = appointments.Count(a => a.ProviderId == p.Id && a.Status == AppointmentStatus.Completed),
                Cancelled = appointments.Count(a => a.ProviderId == p.Id &&
                                                   (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.ClosedBySystem))
            }).ToList();

            var queues = locationId.HasValue
                ? CompositionRoot.Queues.ListByLocation(locationId.Value)
                : CompositionRoot.Queues.ListAll();

            var queueRows = queues.Select(q => new
            {
                QueueId = q.Id,
                QueueName = q.Name,
                Waiting = appointments.Count(a => a.QueueId == q.Id &&
                                                  (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Arrived)),
                InService = appointments.Count(a => a.QueueId == q.Id && a.Status == AppointmentStatus.InService),
                Completed = appointments.Count(a => a.QueueId == q.Id && a.Status == AppointmentStatus.Completed),
                Cancelled = appointments.Count(a => a.QueueId == q.Id &&
                                                   (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.ClosedBySystem))
            }).ToList();

            var dailyTrend = Enumerable.Range(0, 7)
                .Select(i => dayStart.AddDays(i - 6))
                .Select(day => new
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    Booked = appointments.Count(a => a.CreatedUtc >= day && a.CreatedUtc < day.AddDays(1))
                })
                .ToList();

            HandlerUtil.WriteJson(context, new
            {
                ok = true,
                data = new
                {
                    BookedToday = bookedToday,
                    ScheduledToday = scheduledToday,
                    CompletedToday = completedToday,
                    CancelledToday = cancelledToday,
                    Providers = providerRows,
                    Queues = queueRows,
                    DailyTrend = dailyTrend
                }
            });
        }

        public bool IsReusable => true;
    }
}
