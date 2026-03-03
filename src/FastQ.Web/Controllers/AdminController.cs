using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using FastQ.Data.Entities;
using FastQ.Web.Models;
using FastQ.Web.Services;

namespace FastQ.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly AdminService _service;

        public AdminController()
        {
            _service = new AdminService();
        }

        [HttpGet]
        public ActionResult Dashboard()
        {
            return View(BuildDashboardModel());
        }

        [HttpGet]
        public JsonResult AdminSnapshot(string locationId)
        {
            long locId;
            var hasLocation = long.TryParse(locationId, out locId);

            var queues = _service.ListQueues(hasLocation ? (long?)locId : null);

            var providers = _service.ListProviders(hasLocation ? (long?)locId : null);

            var locations = _service.ListLocations();

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
            if (!long.TryParse(queueId, out var qId))
                return Json(new { ok = false, error = "queueId is required" });

            if (!int.TryParse(maxUpcoming, out var maxUpcomingInt) ||
                !int.TryParse(maxDaysAhead, out var maxDaysAheadInt) ||
                !int.TryParse(minHoursLead, out var minHoursLeadInt))
                return Json(new { ok = false, error = "Invalid configuration values" });

            var queue = _service.GetQueue(qId);
            if (queue == null)
                return Json(new { ok = false, error = "Queue not found" });

            if (queue.Config == null)
                queue.Config = new QueueConfig();

            queue.Config.MaxUpcomingAppointments = maxUpcomingInt;
            queue.Config.MaxDaysAhead = maxDaysAheadInt;
            queue.Config.MinHoursLead = minHoursLeadInt;

            _service.UpdateQueue(queue);

            return Json(new { ok = true });
        }

        [HttpPost]
        public JsonResult SystemClose(int staleHours)
        {
            var hours = staleHours <= 0 ? 12 : staleHours;
            var closed = _service.CloseStaleScheduledAppointments(hours);
            return Json(new { ok = true, closed = closed });
        }

        private AdminDashboardViewModel BuildDashboardModel()
        {
            var location = _service.GetPrimaryLocation();
            if (location == null)
            {
                return new AdminDashboardViewModel();
            }

            var rows = BuildAppointmentRows(location);
            var today = DateTime.Today;

            return new AdminDashboardViewModel
            {
                LocationId = location.Id,
                LocationName = location.Name,
                TodayAppointments = rows.Where(r => r.ScheduledForLocal.Date == today).ToList(),
                UpcomingAppointments = rows.Where(r => r.ScheduledForLocal.Date > today).ToList()
            };
        }

        private IList<AdminAppointmentRow> BuildAppointmentRows(Location location)
        {
            var queues = _service.ListQueuesByLocation(location.Id).ToDictionary(q => q.Id, q => q);
            var customers = _service.ListAllCustomers().ToDictionary(c => c.Id, c => c);
            var appointments = _service.ListAppointmentsByLocation(location.Id);

            return appointments
                .Select(a =>
                {
                    queues.TryGetValue(a.QueueId, out var queue);
                    customers.TryGetValue(a.CustomerId, out var customer);

                    var localTime = a.ScheduledForUtc.ToLocalTime();
                    var contact = string.IsNullOrWhiteSpace(a.ContactType)
                        ? (customer != null && customer.SmsOptIn ? "Virtual" : "In-Person")
                        : a.ContactType;

                    return new AdminAppointmentRow
                    {
                        AppointmentId = a.Id,
                        ScheduledForUtc = a.ScheduledForUtc,
                        ScheduledForLocal = localTime,
                        StartTimeText = localTime.ToString("h:mm tt"),
                        StartDateText = localTime.ToString("MMM dd, yyyy"),
                        QueueName = queue?.Name ?? "Unknown Queue",
                        ServiceType = queue?.Name ?? "General",
                        CustomerName = customer?.Name ?? "Unknown",
                        Phone = customer?.Phone ?? "-",
                        Status = a.Status,
                        StatusText = a.Status.ToString().ToUpperInvariant(),
                        ContactMethod = contact,
                        Notes = string.IsNullOrWhiteSpace(a.MoreInfo) ? "No notes added." : a.MoreInfo,
                        MeetingUrl = a.MeetingUrl
                    };
                })
                .OrderBy(r => r.ScheduledForLocal)
                .ToList();
        }
    }
}
