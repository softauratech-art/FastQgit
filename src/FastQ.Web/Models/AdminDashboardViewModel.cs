using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Web.Models
{
    public class AdminDashboardViewModel
    {
        public long LocationId { get; set; }
        public string LocationName { get; set; }
        public DateTime DisplayMonth { get; set; }
        public DateTime SelectedDate { get; set; }
        public IList<AdminAppointmentRow> TodayAppointments { get; set; } = new List<AdminAppointmentRow>();
        public IList<AdminAppointmentRow> UpcomingAppointments { get; set; } = new List<AdminAppointmentRow>();
        public IList<AdminAppointmentRow> SelectedDayAppointments { get; set; } = new List<AdminAppointmentRow>();
        public IList<AdminCalendarDay> CalendarDays { get; set; } = new List<AdminCalendarDay>();
        public IList<AdminOptionItem> QueueOptions { get; set; } = new List<AdminOptionItem>();
        public string FeedbackMessage { get; set; }
        public bool FeedbackIsError { get; set; }
    }

    public class AdminAppointmentRow
    {
        public long AppointmentId { get; set; }
        public string SrcType { get; set; }
        public string StartTimeText { get; set; }
        public string StartDateText { get; set; }
        public string QueueName { get; set; }
        public string ServiceType { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string StatusText { get; set; }
        public AppointmentStatus Status { get; set; }
        public string ContactMethod { get; set; }
        public string EntryKind { get; set; }
        public string Notes { get; set; }
        public string MeetingUrl { get; set; }
        public DateTime ScheduledForUtc { get; set; }
        public DateTime ScheduledForLocal { get; set; }
    }

    public class AdminCalendarDay
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsSelected { get; set; }
        public bool IsToday { get; set; }
        public int AppointmentCount { get; set; }
    }

    public class AdminOptionItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
