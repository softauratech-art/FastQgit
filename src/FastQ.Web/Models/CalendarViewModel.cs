using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Web.Models
{
    public class CalendarViewModel
    {
        public long EntityId { get; set; }
        public string EntityName { get; set; }
        public DateTime DisplayMonth { get; set; }
        public DateTime SelectedDate { get; set; }
        public IList<CalendarAppointmentRow> TodayAppointments { get; set; } = new List<CalendarAppointmentRow>();
        public IList<CalendarAppointmentRow> UpcomingAppointments { get; set; } = new List<CalendarAppointmentRow>();
        public IList<CalendarAppointmentRow> SelectedDayAppointments { get; set; } = new List<CalendarAppointmentRow>();
        public IList<CalendarAppointmentRow> MonthAppointments { get; set; } = new List<CalendarAppointmentRow>();
        public IList<CalendarDay> CalendarDays { get; set; } = new List<CalendarDay>();
        public IList<SelectOptionItem> QueueOptions { get; set; } = new List<SelectOptionItem>();
        public string FeedbackMessage { get; set; }
        public bool FeedbackIsError { get; set; }
    }

    public class CalendarAppointmentRow
    {
        public long AppointmentId { get; set; }
        public long QueueId { get; set; }
        public long ServiceId { get; set; }
        public string SrcType { get; set; }
        public string StartTimeText { get; set; }
        public string StartDateText { get; set; }
        public string QueueName { get; set; }
        public string ServiceType { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string Phone { get; set; }
        public string StatusText { get; set; }
        public AppointmentStatus Status { get; set; }
        public string ContactMethod { get; set; }
        public string LanguagePreference { get; set; }
        public string RefValue { get; set; }
        public string StampUser { get; set; }
        public string StampUserName { get; set; }
        public string EntryKind { get; set; }
        public string Notes { get; set; }
        public string MeetingUrl { get; set; }
        public string MeetingUrlHost { get; set; }
        public DateTime ScheduledFor { get; set; }
    }

    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsSelected { get; set; }
        public bool IsToday { get; set; }
        public int AppointmentCount { get; set; }
    }

    public class SelectOptionItem
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
