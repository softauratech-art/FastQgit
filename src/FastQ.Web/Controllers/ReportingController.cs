using FastQ.Data.Entities;
using FastQ.Web.Attributes;
using FastQ.Web.Helpers;
using FastQ.Web.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;

namespace FastQ.Web.Controllers
{
    [FQAuthorizeUser(AllowRole = $"{nameof(Utilities.FQRole.Reporter)},{nameof(Utilities.FQRole.QueueAdmin)},{nameof(Utilities.FQRole.SuperAdmin)}")]
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
        public JsonResult ReportingSnapshot(string entityId, string queueId, string period, string startDate, string endDate)
        {
            long parsedEntityId;
            long qId;
            var hasEntity = long.TryParse(entityId, out parsedEntityId);
            var hasQueue = long.TryParse(queueId, out qId);

            var appointments = _service.ListAppointments(hasEntity ? (long?)parsedEntityId : null).ToList();

            if (hasQueue)
                appointments = appointments.Where(a => a.QueueId == qId).ToList();

            var now = DateTime.Now;
            var range = ResolvePeriodRange(period, startDate, endDate, now);
            var filtered = appointments.Where(a => a.ScheduledFor >= range.Start && a.ScheduledFor < range.EndExclusive).ToList();

            var bookedToday = appointments.Count(a => a.CreatedOn >= range.Start && a.CreatedOn < range.EndExclusive);
            var scheduledToday = appointments.Count(a => a.ScheduledFor >= range.Start && a.ScheduledFor < range.EndExclusive);
            var completedToday = appointments.Count(a => a.UpdatedOn >= range.Start && a.UpdatedOn < range.EndExclusive && a.Status == AppointmentStatus.Completed);
            var cancelledToday = appointments.Count(a => a.UpdatedOn >= range.Start && a.UpdatedOn < range.EndExclusive &&
                                                       (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.ClosedBySystem));

            var providers = _service.ListProviders(hasEntity ? (long?)parsedEntityId : null);

            var providerRows = providers.Select(p => new
            {
                ProviderId = p.Id,
                ProviderName = p.Name,
                Arrived = filtered.Count(a => a.ProviderId == p.Id && a.Status == AppointmentStatus.Arrived),
                InService = filtered.Count(a => a.ProviderId == p.Id && a.Status == AppointmentStatus.InService),
                Completed = filtered.Count(a => a.ProviderId == p.Id && a.Status == AppointmentStatus.Completed),
                Cancelled = filtered.Count(a => a.ProviderId == p.Id &&
                                                   (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.ClosedBySystem))
            }).ToList();

            var queues = _service.ListQueues(hasEntity ? (long?)parsedEntityId : null);

            var queueRows = queues.Select(q => new
            {
                QueueId = q.Id,
                QueueName = q.Name,
                Waiting = filtered.Count(a => a.QueueId == q.Id &&
                                                  (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Arrived)),
                InService = filtered.Count(a => a.QueueId == q.Id && a.Status == AppointmentStatus.InService),
                Completed = filtered.Count(a => a.QueueId == q.Id && a.Status == AppointmentStatus.Completed),
                Cancelled = filtered.Count(a => a.QueueId == q.Id &&
                                                   (a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.ClosedBySystem))
            }).ToList();

            var dailyTrend = Enumerable.Range(0, 7)
                .Select(i => dayStart.AddDays(i - 6))
                .Select(day => new
                {
                    Date = day.ToString("yyyy-MM-dd"),
                    Booked = appointments.Count(a => a.CreatedOn >= day && a.CreatedOn < day.AddDays(1))
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
                    DailyTrend = dailyTrend,
                    FilterPeriod = range.Period,
                    FilterStart = range.Start.ToString("yyyy-MM-dd"),
                    FilterEnd = range.EndExclusive.AddDays(-1).ToString("yyyy-MM-dd")
                }
            }, JsonRequestBehavior.AllowGet);
        }

        private static (DateTime Start, DateTime EndExclusive, string Period) ResolvePeriodRange(string period, string startDate, string endDate, DateTime now)
        {
            var normalized = (period ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized != "daily" && normalized != "weekly" && normalized != "monthly" && normalized != "range")
                normalized = "daily";

            if (normalized == "range")
            {
                if (TryParseDate(startDate, out var from) && TryParseDate(endDate, out var to))
                {
                    if (to < from)
                    {
                        var swap = from;
                        from = to;
                        to = swap;
                    }

                    return (from.Date, to.Date.AddDays(1), normalized);
                }

                normalized = "daily";
            }

            if (normalized == "weekly")
            {
                var firstDay = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
                var start = now.Date;
                while (start.DayOfWeek != firstDay)
                    start = start.AddDays(-1);
                return (start, start.AddDays(7), normalized);
            }

            if (normalized == "monthly")
            {
                var start = new DateTime(now.Year, now.Month, 1);
                return (start, start.AddMonths(1), normalized);
            }

            var dayStart = now.Date;
            return (dayStart, dayStart.AddDays(1), "daily");
        }

        private static bool TryParseDate(string value, out DateTime parsed)
        {
            return DateTime.TryParseExact((value ?? string.Empty).Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
        }
    }
}
