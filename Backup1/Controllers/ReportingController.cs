using System;
using System.Linq;
using System.Web.Mvc;
using FastQ.Data.Entities;
using FastQ.Web.Services;

namespace FastQ.Web.Controllers
{
    public class ReportingController : Controller
    {
        private readonly ReportingService _service;

        public ReportingController()
        {
            _service = new ReportingService();
        }

        [HttpGet]
        public ActionResult Overview()
        {
            return View();
        }

        [HttpGet]
        public JsonResult ReportingSnapshot(string locationId, string queueId)
        {
            Guid locId;
            Guid qId;
            var hasLocation = Guid.TryParse(locationId, out locId);
            var hasQueue = Guid.TryParse(queueId, out qId);

            var appointments = _service.ListAppointments(hasLocation ? locId : (Guid?)null).ToList();

            if (hasQueue)
                appointments = appointments.Where(a => a.QueueId == qId).ToList();

            var now = DateTime.UtcNow;
            var dayStart = now.Date;
            var dayEnd = dayStart.AddDays(1);

            var bookedToday = appointments.Count(a => a.CreatedUtc >= dayStart && a.CreatedUtc < dayEnd);
            var scheduledToday = appointments.Count(a => a.ScheduledForUtc >= dayStart && a.ScheduledForUtc < dayEnd);
            var completedToday = appointments.Count(a => a.UpdatedUtc >= dayStart && a.UpdatedUtc < dayEnd && a.Status == AppointmentStatus.Completed);
            var cancelledToday = appointments.Count(a => a.UpdatedUtc >= dayStart && a.UpdatedUtc < dayEnd &&
                                                       (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.ClosedBySystem));

            var providers = _service.ListProviders(hasLocation ? locId : (Guid?)null);

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

            var queues = _service.ListQueues(hasLocation ? locId : (Guid?)null);

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

            return Json(new
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
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
