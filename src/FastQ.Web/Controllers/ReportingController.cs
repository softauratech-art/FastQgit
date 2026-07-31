using FastQ.Data.Entities;
using FastQ.Web.Attributes;
using FastQ.Web.Helpers;
using FastQ.Web.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace FastQ.Web.Controllers
{
    [FQAuthorizeUser(AllowRole = $"{nameof(Utilities.FQRole.Reporter)},{nameof(Utilities.FQRole.QueueAdmin)},{nameof(Utilities.FQRole.SuperAdmin)}")]
    public class ReportingController : Controller
    {
        private readonly ReportingService _service;

        private class ProviderReportRow
        {
            public string ProviderId { get; set; }
            public string ProviderName { get; set; }
            public int Arrived { get; set; }
            public int InService { get; set; }
            public int Completed { get; set; }
            public int Cancelled { get; set; }
        }

        private class QueueReportRow
        {
            public long QueueId { get; set; }
            public string QueueName { get; set; }
            public int Waiting { get; set; }
            public int InService { get; set; }
            public int Completed { get; set; }
            public int Cancelled { get; set; }
            public int ClosedBySystem { get; set; }
            public int Other { get; set; }
        }

        private class TrendReportRow
        {
            public string Date { get; set; }
            public int Booked { get; set; }
            public int Scheduled { get; set; }
        }

        private class ReportSnapshotData
        {
            public int BookedToday { get; set; }
            public int ScheduledToday { get; set; }
            public int CompletedToday { get; set; }
            public int CancelledToday { get; set; }
            public List<ProviderReportRow> Providers { get; set; }
            public List<QueueReportRow> Queues { get; set; }
            public List<TrendReportRow> DailyTrend { get; set; }
            public string FilterPeriod { get; set; }
            public string FilterStart { get; set; }
            public string FilterEnd { get; set; }
        }

        private class MetricsSnapshotData
        {
            public string Granularity { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public long QueueId { get; set; }
            public List<QueueLengthsReport> QueueLengths { get; set; }
            public List<AverageServiceDurationReport> ServiceDurations { get; set; }
        }

        public ReportingController()
        {
            _service = new ReportingService();
        }

        [HttpGet]
        public ActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Metrics()
        {
            return View();
        }

        [HttpGet]
        public JsonResult MetricsSnapshot(string queueId, string granularity, string startDate, string endDate, string srcType)
        {
            DateTime parsedStart;
            DateTime parsedEnd;
            if (!DateTime.TryParse(startDate, out parsedStart) || !DateTime.TryParse(endDate, out parsedEnd))
                return Json(new { ok = false, error = "A valid start and end date are required." }, JsonRequestBehavior.AllowGet);

            long parsedQueueId;
            long? qId = long.TryParse(queueId, out parsedQueueId) && parsedQueueId > 0 ? (long?)parsedQueueId : null;
            var data = new MetricsSnapshotData
            {
                QueueLengths = _service.ListQueueLengths(parsedStart, parsedEnd, granularity, qId, srcType).ToList(),
                ServiceDurations = _service.ListServiceDurations(parsedStart, parsedEnd, granularity, qId).ToList(),
                Granularity = granularity,
                StartDate = startDate,
                EndDate = endDate,
                QueueId = qId ?? 0
            };
            return Json(new { ok = true, data }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Overview()
        {
            return View();
        }

        [HttpGet]
        public JsonResult ReportingSnapshot(string entityId, string queueId, string period, string startDate, string endDate)
        {
            var data = BuildSnapshotData(entityId, queueId, period, startDate, endDate);

            return Json(new
            {
                ok = true,
                data
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public FileResult ExportCsv(string entityId, string queueId, string period, string startDate, string endDate)
        {
            var data = BuildSnapshotData(entityId, queueId, period, startDate, endDate);
            var csv = BuildDelimitedReport(data, ",");
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(csv);
            var fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpGet]
        public FileResult ExportExcel(string entityId, string queueId, string period, string startDate, string endDate)
        {
            var data = BuildSnapshotData(entityId, queueId, period, startDate, endDate);
            var tsv = BuildDelimitedReport(data, "\t");
            var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(tsv);
            var fileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
            return File(bytes, "application/vnd.ms-excel", fileName);
        }

        private ReportSnapshotData BuildSnapshotData(string entityId, string queueId, string period, string startDate, string endDate)
        {
            long parsedEntityId;
            long qId;
            var hasEntity = long.TryParse(entityId, out parsedEntityId);
            var hasQueue = long.TryParse(queueId, out qId);
            var now = DateTime.Now;
            var dayStart = now.Date;
            var range = ResolvePeriodRange(period, startDate, endDate, now);
            var appointments = _service.ListAppointments(hasEntity ? (long?)parsedEntityId : null).ToList();
            var userId = new AuthService().GetLoggedInWindowsUser();
            var allEntries = _service.ListAppointmentsWalkins(hasEntity ? (long?)parsedEntityId : null, userId, range.Start, range.EndExclusive).ToList();

            if (hasQueue)
            {
                appointments = appointments.Where(a => a.QueueId == qId).ToList();
                allEntries = allEntries.Where(a => a.QueueId == qId).ToList();
            }

            var filtered = allEntries.Where(a => a.ScheduledFor >= range.Start && a.ScheduledFor < range.EndExclusive).ToList();

            var providers = _service.ListProviders(hasEntity ? (long?)parsedEntityId : null);
            var queues = _service.ListQueues(hasEntity ? (long?)parsedEntityId : null);

            return new ReportSnapshotData
            {
                BookedToday = appointments.Count(a => a.CreatedOn >= range.Start && a.CreatedOn < range.EndExclusive),
                ScheduledToday = allEntries.Count(a => a.ScheduledFor >= range.Start && a.ScheduledFor < range.EndExclusive),
                CompletedToday = allEntries.Count(a => a.UpdatedOn >= range.Start && a.UpdatedOn < range.EndExclusive && a.Status == AppointmentStatus.Completed),
                CancelledToday = allEntries.Count(a => a.UpdatedOn >= range.Start && a.UpdatedOn < range.EndExclusive && a.Status == AppointmentStatus.Cancelled),
                Providers = (providers.Select(p => new ProviderReportRow
                {
                    ProviderId = p.Id,
                    ProviderName = p.Name,
                    Arrived = filtered.Count(a => a.StampUser == p.Id && a.Status == AppointmentStatus.Arrived),
                    InService = filtered.Count(a => a.StampUser == p.Id && a.Status == AppointmentStatus.InService),
                    Completed = filtered.Count(a => a.StampUser == p.Id && a.Status == AppointmentStatus.Completed),
                    Cancelled = filtered.Count(a => a.StampUser == p.Id && a.Status == AppointmentStatus.Cancelled)                    
                })).Where(u => u.Arrived > 0 || u.InService > 0 || u.Completed > 0 || u.Cancelled > 0).ToList(),
                Queues = (queues.Select(q => new QueueReportRow
                {
                    QueueId = q.Id,
                    QueueName = q.Name,
                    Waiting = filtered.Count(a => a.QueueId == q.Id &&
                                                  (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Arrived)),
                    InService = filtered.Count(a => a.QueueId == q.Id && a.Status == AppointmentStatus.InService),
                    Completed = filtered.Count(a => a.QueueId == q.Id && a.Status == AppointmentStatus.Completed),
                    Cancelled = filtered.Count(a => a.QueueId == q.Id && a.Status == AppointmentStatus.Cancelled),
                    ClosedBySystem = filtered.Count(a => a.QueueId == q.Id && a.Status == AppointmentStatus.ClosedBySystem),
                    Other = filtered.Count(a => a.QueueId == q.Id &&
                        a.Status != AppointmentStatus.Scheduled && a.Status != AppointmentStatus.Arrived &&
                        a.Status != AppointmentStatus.InService && a.Status != AppointmentStatus.Completed &&
                        a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.ClosedBySystem)
                })).Where(fq => fq.Waiting > 0 || fq.InService > 0 || fq.Completed > 0 || fq.Cancelled > 0 || fq.ClosedBySystem > 0).ToList(),
                DailyTrend = Enumerable.Range(0, 7)
                    .Select(i => dayStart.AddDays(i - 6))
                    .Select(day => new TrendReportRow
                    {
                        Date = day.ToString("yyyy-MM-dd"),
                        Booked = appointments.Count(a => a.CreatedOn >= day && a.CreatedOn < day.AddDays(1)),
                        Scheduled = appointments.Count(a => a.ApptDate >= day && a.ApptDate < day.AddDays(1))
                    })
                    .ToList(),
                FilterPeriod = range.Period,
                FilterStart = range.Start.ToString("yyyy-MM-dd"),
                FilterEnd = range.EndExclusive.AddDays(-1).ToString("yyyy-MM-dd")
            };
        }

        private static string BuildDelimitedReport(ReportSnapshotData data, string delimiter)
        {
            var sb = new StringBuilder();

            void AddLine(params string[] values)
            {
                sb.AppendLine(string.Join(delimiter, values.Select(v => EscapeCell(v, delimiter))));
            }

            AddLine("Report", "FastQ Reporting");
            AddLine("Period", data.FilterPeriod ?? string.Empty);
            AddLine("Start", data.FilterStart ?? string.Empty);
            AddLine("End", data.FilterEnd ?? string.Empty);
            AddLine();

            AddLine("Summary");
            AddLine("Booked", "Scheduled", "Completed", "Cancelled");
            AddLine(data.BookedToday.ToString(), data.ScheduledToday.ToString(), data.CompletedToday.ToString(), data.CancelledToday.ToString());
            AddLine();
           
            AddLine("Provider Activity");
            AddLine("Provider", "Arrived", "In Service", "Completed", "Cancelled");
            foreach (var row in data.Providers ?? new List<ProviderReportRow>())
                AddLine(row.ProviderName, row.Arrived.ToString(), row.InService.ToString(), row.Completed.ToString(), row.Cancelled.ToString());
            AddLine();

            AddLine("Queue Breakdown");
            AddLine("Queue", "Waiting", "In Service", "Completed", "Cancelled");
            foreach (var row in data.Queues ?? new List<QueueReportRow>())
                AddLine(row.QueueName, row.Waiting.ToString(), row.InService.ToString(), row.Completed.ToString(), row.Cancelled.ToString());
            AddLine();

            AddLine("7 Day Booking Trend");
            AddLine("Date", "Booked");
            foreach (var row in data.DailyTrend ?? new List<TrendReportRow>())
                AddLine(row.Date, row.Booked.ToString());

            return sb.ToString();
        }

        private static string EscapeCell(string value, string delimiter)
        {
            var safe = value ?? string.Empty;
            if (delimiter == ",")
            {
                if (safe.Contains(",") || safe.Contains("\"") || safe.Contains("\n") || safe.Contains("\r"))
                    return "\"" + safe.Replace("\"", "\"\"") + "\"";
            }
            return safe;
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
