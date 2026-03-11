using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FastQ.Data.Common;
using FastQ.Data.Db;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Web.Models;

namespace FastQ.Web.Services
{
    public class CalendarService
    {
        private readonly ProviderService _providerService;
        private readonly CustomerService _customerService;
        private readonly IQueueRepository _queues;

        public CalendarService()
            : this(
                new ProviderService(),
                new CustomerService(),
                DbRepositoryFactory.CreateQueueRepository())
        {
        }

        public CalendarService(
            ProviderService providerService,
            CustomerService customerService,
            IQueueRepository queues)
        {
            _providerService = providerService;
            _customerService = customerService;
            _queues = queues;
        }

        public AdminDashboardViewModel BuildCalendarModel(string userId, DateTime displayMonth, DateTime selectedDate)
        {
            var monthStart = new DateTime(displayMonth.Year, displayMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var selected = selectedDate.Date;

            var rows = new List<AdminAppointmentRow>();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                rows.AddRange(_providerService.BuildRowsForUser(userId, monthStart, monthEnd).Select(r => MapRow(r, "A", "Appointment")));
                rows.AddRange(_providerService.BuildWalkinsForUser(userId, monthStart, monthEnd).Select(r => MapRow(r, "W", "Walk-In")));
            }

            var counts = rows
                .GroupBy(r => r.ScheduledForUtc.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var model = new AdminDashboardViewModel
            {
                DisplayMonth = monthStart,
                SelectedDate = selected,
                LocationName = string.IsNullOrWhiteSpace(userId) ? "Assigned provider schedule" : userId,
                QueueOptions = _providerService.ListQueues()
                    .OrderBy(q => q.Name)
                    .Select(q => new AdminOptionItem
                    {
                        Value = q.Id.ToString(CultureInfo.InvariantCulture),
                        Text = q.Name
                    })
                    .ToList(),
                CalendarDays = BuildCalendarDays(monthStart, selected, counts),
                SelectedDayAppointments = rows
                    .Where(r => r.ScheduledForUtc.Date == selected)
                    .OrderBy(r => r.ScheduledForUtc)
                    .ToList()
            };

            return model;
        }

        public Result<Appointment> CreateScheduledAppointment(
            long queueId,
            string serviceId,
            string refValue,
            string permitNumber,
            string email,
            string customerName,
            string phone,
            string contactType,
            DateTime scheduledForUtc,
            string notes,
            string meetingUrl)
        {
            return _customerService.CreateScheduled(
                queueId,
                serviceId,
                refValue,
                permitNumber,
                email,
                customerName,
                phone,
                contactType,
                scheduledForUtc,
                notes,
                meetingUrl,
                "web");
        }

        public Result<long> CreateWalkin(
            long queueId,
            string serviceId,
            string refValue,
            string permitNumber,
            string email,
            string customerName,
            string phone,
            string contactType,
            string meetingUrl,
            string notes)
        {
            return _customerService.CreateWalkin(
                queueId,
                serviceId,
                refValue,
                permitNumber,
                email,
                customerName,
                phone,
                contactType,
                meetingUrl,
                notes,
                "web");
        }

        private static IList<AdminCalendarDay> BuildCalendarDays(DateTime monthStart, DateTime selectedDate, IDictionary<DateTime, int> counts)
        {
            var gridStart = monthStart.AddDays(-(int)monthStart.DayOfWeek);
            var days = new List<AdminCalendarDay>(42);

            for (var i = 0; i < 42; i++)
            {
                var date = gridStart.AddDays(i).Date;
                days.Add(new AdminCalendarDay
                {
                    Date = date,
                    IsCurrentMonth = date.Month == monthStart.Month && date.Year == monthStart.Year,
                    IsSelected = date == selectedDate.Date,
                    IsToday = date == DateTime.Today,
                    AppointmentCount = counts.TryGetValue(date, out var count) ? count : 0
                });
            }

            return days;
        }

        private static AdminAppointmentRow MapRow(ProviderAppointmentRow row, string srcType, string entryKind)
        {
            return new AdminAppointmentRow
            {
                AppointmentId = row.AppointmentId,
                QueueId = row.QueueId,
                SrcType = srcType,
                StartTimeText = row.StartTimeText,
                StartDateText = row.StartDateText,
                QueueName = row.QueueName,
                ServiceType = row.ServiceType,
                CustomerName = row.CustomerName,
                Phone = row.Phone,
                StatusText = row.StatusText,
                Status = row.Status,
                ContactMethod = row.ContactMethod,
                StampUser = row.StampUser,
                EntryKind = entryKind,
                ScheduledForUtc = row.ScheduledForUtc,
                ScheduledForLocal = row.ScheduledForUtc,
                Notes = "Use Info to add meeting details or notes.",
                MeetingUrl = string.Empty
            };
        }
    }
}
