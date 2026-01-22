using System;
using System.Linq;
using System.Web.Mvc;
using FastQ.Data.Entities;
using FastQ.Web.App_Start;
using FastQ.Web.Models;

namespace FastQ.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminService _admin;
        private readonly SystemCloseService _systemClose;

        public AdminController()
        {
            _admin = CompositionRoot.Admin;
            _systemClose = CompositionRoot.SystemClose;
        }

        [HttpGet]
        public ActionResult Dashboard()
        {
            var locations = _admin.ListLocations();
            var location = locations.FirstOrDefault();
            if (location == null)
                return View(new AdminDashboardViewModel());

            var queues = _admin.ListQueuesByLocation(location.Id).ToList();
            var customers = _admin.ListAllCustomers().ToList();
            var appointments = _admin.ListAppointmentsByLocation(location.Id).ToList();

            var queueMap = queues.ToDictionary(q => q.Id, q => q);
            var customerMap = customers.ToDictionary(c => c.Id, c => c);

            var rows = appointments.Select(a =>
            {
                queueMap.TryGetValue(a.QueueId, out var queue);
                customerMap.TryGetValue(a.CustomerId, out var customer);

                var localTime = a.ScheduledForUtc.ToLocalTime();
                var contact = customer != null && customer.SmsOptIn ? "Online" : "In-Person";

                return new AdminAppointmentRow
                {
                    AppointmentId = a.Id,
                    ScheduledForUtc = a.ScheduledForUtc,
                    StartTimeText = localTime.ToString("h:mm tt"),
                    StartDateText = localTime.ToString("MMM dd, yyyy"),
                    QueueName = queue?.Name ?? "Unknown Queue",
                    ServiceType = queue?.Name != null ? $"Questions: {queue.Name}" : "Questions: General",
                    CustomerName = customer?.Name ?? "Unknown",
                    Phone = customer?.Phone ?? "-",
                    Status = a.Status,
                    StatusText = a.Status.ToString().ToUpperInvariant(),
                    ContactMethod = contact
                };
            }).OrderBy(r => r.ScheduledForUtc).ToList();

            var today = DateTime.UtcNow.Date;
            var model = new AdminDashboardViewModel
            {
                LocationName = location.Name,
                TodayAppointments = rows.Where(r => r.ScheduledForUtc.Date == today).ToList(),
                UpcomingAppointments = rows.Where(r => r.ScheduledForUtc.Date > today).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public JsonResult AdminSnapshot(string locationId)
        {
            Guid locId;
            var hasLocation = Guid.TryParse(locationId, out locId);

            var queues = _admin.ListQueues(hasLocation ? locId : (Guid?)null);

            var providers = _admin.ListProviders(hasLocation ? locId : (Guid?)null);

            var locations = _admin.ListLocations();

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

            return Json(new
            {
                ok = true,
                data = new
                {
                    Queues = queueRows,
                    Providers = providerRows
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AdminUpdate(string queueId, string maxUpcoming, string maxDaysAhead, string minHoursLead)
        {
            if (!Guid.TryParse(queueId, out var qId))
                return Json(new { ok = false, error = "queueId is required" });

            if (!int.TryParse(maxUpcoming, out var maxUpcomingInt) ||
                !int.TryParse(maxDaysAhead, out var maxDaysAheadInt) ||
                !int.TryParse(minHoursLead, out var minHoursLeadInt))
                return Json(new { ok = false, error = "Invalid configuration values" });

            var queue = _admin.GetQueue(qId);
            if (queue == null)
                return Json(new { ok = false, error = "Queue not found" });

            if (queue.Config == null)
                queue.Config = new QueueConfig();

            queue.Config.MaxUpcomingAppointments = maxUpcomingInt;
            queue.Config.MaxDaysAhead = maxDaysAheadInt;
            queue.Config.MinHoursLead = minHoursLeadInt;

            _admin.UpdateQueue(queue);

            return Json(new { ok = true });
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
