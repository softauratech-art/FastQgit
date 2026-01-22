using System;
using System.Globalization;
using System.Linq;
using System.Web.Configuration;
using System.Web.Mvc;
using FastQ.Data.Entities;
using FastQ.Data.Common;
using FastQ.Data.Oracle;
using FastQ.Web.Models;
using FastQ.Web.Helpers;
using FastQ.Web.Services;

namespace FastQ.Web.Controllers
{
    public class ProviderController : Controller
    {
        private readonly ProviderScheduleService _schedule;
        private readonly QueueQueryService _queries;
        private readonly ProviderService _provider;
        private readonly TransferService _transfer;
        private readonly SystemCloseService _systemClose;

        public ProviderController()
        {
            var connString = GetConnectionString();

            var appts = new OracleAppointmentRepository(connString);
            var customers = new OracleCustomerRepository(connString);
            var queues = new OracleQueueRepository(connString);
            var locations = new OracleLocationRepository(connString);

            IClock clock = new SystemClock();
            IRealtimeNotifier notifier = new SignalRRealtimeNotifier();

            _schedule = new ProviderScheduleService(appts, customers, queues);
            _queries = new QueueQueryService(appts, customers, queues, locations);
            _provider = new ProviderService(appts, clock, notifier);
            _transfer = new TransferService(appts, queues, clock, notifier);
            _systemClose = new SystemCloseService(appts, clock, notifier);
        }

        private static string GetConnectionString()
        {
            var connString = WebConfigurationManager.ConnectionStrings["FastQOracle"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connString))
                throw new InvalidOperationException("FastQOracle connection string is missing.");

            return connString;
        }

        [HttpGet]
        public ActionResult Today()
        {
            var today = DateTime.UtcNow.Date;

            var queues = _schedule.ListQueues().ToList();
            var customers = _schedule.ListCustomers().ToList();
            var appointments = _schedule.ListAppointmentsForDate(today).ToList();

            var queueMap = queues.ToDictionary(q => q.Id, q => q);
            var customerMap = customers.ToDictionary(c => c.Id, c => c);

            var rows = appointments.Select(a =>
            {
                queueMap.TryGetValue(a.QueueId, out var queue);
                customerMap.TryGetValue(a.CustomerId, out var customer);

                var contact = customer != null && customer.SmsOptIn ? "Online" : "In-Person";

                return new ProviderAppointmentRow
                {
                    AppointmentId = a.Id,
                    ScheduledForUtc = a.ScheduledForUtc,
                    StartTimeText = a.ScheduledForUtc.ToString("h:mm tt", CultureInfo.InvariantCulture),
                    StartDateText = a.ScheduledForUtc.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture),
                    QueueName = queue?.Name ?? "Unknown Queue",
                    ServiceType = queue?.Name != null ? $"Questions: {queue.Name}" : "Questions: General",
                    CustomerName = customer?.Name ?? "Unknown",
                    Phone = customer?.Phone ?? "-",
                    Status = a.Status,
                    StatusText = a.Status.ToString().ToUpperInvariant(),
                    ContactMethod = contact
                };
            }).OrderBy(r => r.ScheduledForUtc).ToList();

            var model = new ProviderTodayViewModel
            {
                DateText = DateTime.UtcNow.ToString("ddd, MMM dd yyyy", CultureInfo.InvariantCulture) + " (UTC)",
                LiveQueue = rows.Where(r => r.Status == AppointmentStatus.Arrived || r.Status == AppointmentStatus.InService).ToList(),
                Scheduled = rows.Where(r => r.Status != AppointmentStatus.Arrived && r.Status != AppointmentStatus.InService).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public JsonResult GetQueueSnapshot(string locationId, string queueId)
        {
            if (!Guid.TryParse(locationId, out var locId) || !Guid.TryParse(queueId, out var qId))
                return Json(new { ok = false, error = "locationId and queueId are required" }, JsonRequestBehavior.AllowGet);

            var dto = _queries.GetQueueSnapshot(locId, qId);
            return Json(new { ok = true, data = dto }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult ProviderAction(string action, string appointmentId, string providerId)
        {
            if (string.IsNullOrWhiteSpace(action) || !Guid.TryParse(appointmentId, out var apptId))
                return Json(new { ok = false, error = "action and appointmentId are required" });

            action = action.Trim().ToLowerInvariant();
            if (!Guid.TryParse(providerId, out var provId))
                provId = Guid.Parse("02a62b1c-0000-0000-4641-535451494430");

            var res = action switch
            {
                "arrive" => _provider.MarkArrived(apptId),
                "begin" => _provider.BeginService(apptId, provId),
                "end" => _provider.EndService(apptId),
                _ => Result.Fail("Unknown action")
            };

            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true });
        }

        [HttpPost]
        public JsonResult TransferAppointment(string appointmentId, string targetQueueId)
        {
            if (!Guid.TryParse(appointmentId, out var apptId) || !Guid.TryParse(targetQueueId, out var queueId))
                return Json(new { ok = false, error = "appointmentId and targetQueueId are required" });

            var res = _transfer.Transfer(apptId, queueId);
            if (!res.Ok)
                return Json(new { ok = false, error = res.Error });

            return Json(new { ok = true, newAppointmentId = res.Value.Id, newQueueId = res.Value.QueueId });
        }

        [HttpPost]
        public JsonResult SystemClose(int staleHours)
        {
            var hours = staleHours <= 0 ? 12 : staleHours;
            var closed = _systemClose.CloseStaleScheduledAppointments(hours);
            return Json(new { ok = true, closed = closed });
        }
    }
}

