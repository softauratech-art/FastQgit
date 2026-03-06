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
