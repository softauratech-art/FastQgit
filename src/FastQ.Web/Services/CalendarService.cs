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

        public CalendarViewModel BuildCalendarModel(long entityId, string userId, DateTime displayMonth, DateTime selectedDate, string selectedEntry, string selectedQueue)
        {
            selectedEntry = selectedEntry ?? "both";
            selectedQueue = selectedQueue ?? "all";
            
            var monthStart = new DateTime(displayMonth.Year, displayMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var selected = selectedDate.Date;

            var rows = new List<CalendarAppointmentRow>();
            if (!string.IsNullOrWhiteSpace(userId))
            {
                if (selectedEntry.ToLower().Equals("a") || selectedEntry.Equals("both"))
                    rows.AddRange(_providerService.BuildRowsForUser(entityId, userId, monthStart, monthEnd).Select(r => MapRow(r, "A", "Appointment")));
                if (selectedEntry.ToLower().Equals("w") || selectedEntry.Equals("both")) 
                    rows.AddRange(_providerService.BuildWalkinsForUser(entityId,userId, monthStart, monthEnd).Select(r => MapRow(r, "W", "Walk-In")));
            }

            var oMonthAppointments = rows
                    .Where(r => selectedQueue.Equals("all") || r.QueueId == Convert.ToInt64(selectedQueue))
                    .OrderBy(r => r.ScheduledFor)
                    .ToList();

            var counts = oMonthAppointments
                .GroupBy(r => r.ScheduledFor.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var model = new CalendarViewModel
            {
                DisplayMonth = monthStart,
                SelectedDate = selected,
                EntityName = string.IsNullOrWhiteSpace(userId) ? "Assigned provider schedule" : userId,
                QueueOptions = _providerService.ListQueues()
                    .OrderBy(q => q.Name)
                    .Select(q => new SelectOptionItem
                    {
                        Value = q.Id.ToString(CultureInfo.InvariantCulture),
                        Text = q.Name
                    })
                    .ToList(),
                CalendarDays = BuildCalendarDays(monthStart, selected, counts),
                MonthAppointments = rows
                    .Where(r => selectedQueue.Equals("all") || r.QueueId == Convert.ToInt64(selectedQueue) )
                    .OrderBy(r => r.ScheduledFor)
                    .ToList(),
                SelectedDayAppointments = rows
                    .Where(r => r.ScheduledFor.Date == selected && (selectedQueue.Equals("all") || r.QueueId == Convert.ToInt64(selectedQueue)))
                    .OrderBy(r => r.ScheduledFor)
                    .ToList()
            };

            return model;
        }

        private static IList<CalendarDay> BuildCalendarDays(DateTime monthStart, DateTime selectedDate, IDictionary<DateTime, int> counts)
        {
            var gridStart = monthStart.AddDays(-(int)monthStart.DayOfWeek);
            var days = new List<CalendarDay>(42);

            for (var i = 0; i < 42; i++)
            {
                var date = gridStart.AddDays(i).Date;
                days.Add(new CalendarDay
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

        private static CalendarAppointmentRow MapRow(ProviderAppointmentRow row, string srcType, string entryKind)
        {
            return new CalendarAppointmentRow
            {
                AppointmentId = row.AppointmentId,
                QueueId = row.QueueId,
                ServiceId = row.ServiceId,
                SrcType = srcType,
                StartTimeText = row.StartTimeText,
                StartDateText = row.StartDateText,
                QueueName = row.QueueName,
                ServiceType = row.ServiceType,
                CustomerName = row.CustomerName,
                CustomerEmail = row.CustomerEmail,
                Phone = row.Phone,
                StatusText = row.StatusText,
                Status = row.Status,
                ContactMethod = row.ContactMethod,
                LanguagePreference = row.LanguagePreference,
                RefValue = row.RefValue,
                StampUser = row.StampUser,
                EntryKind = entryKind,
                ScheduledFor = row.ScheduledFor,
                Notes = row.Notes,
                ServiceNotes = row.ServiceNotes,
                ServiceStartTimeText = row.ServiceStartTimeText,
                ServiceEndTimeText = row.ServiceEndTimeText,
                ServiceStampUser = row.ServiceStampUser,
                SmsOptIn = row.SmsOptIn,
                MeetingUrl = row.MeetingUrl,
                MeetingUrlHost = row.MeetingUrlHost,
                StampUserName = row.StampUserName
                
            };
        }
    }
}
