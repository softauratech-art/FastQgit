using System;
using FastQ.Data.Entities;

namespace FastQ.Web.Models
{
    public class ProviderAppointmentRow
    {
        public long AppointmentId { get; set; }
        public long QueueId { get; set; }
        public long ServiceId { get; set; }
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
        public string ContactTypeCode { get; set; }
        public string RefCriteria { get; set; }
        public string RefValue { get; set; }
        public string LanguagePreference { get; set; }
        public string MeetingUrl { get; set; }
        public string MeetingUrlHost { get; set; }
        public string Notes { get; set; }
        public string ServiceNotes { get; set; }
        public string ServiceCheckinTimeText { get; set; }
        public string ServiceStartTimeText { get; set; }
        public string ServiceEndTimeText { get; set; }
        public string ServiceStampUser { get; set; }
        public bool SmsOptIn { get; set; }
        public string StampUser { get; set; }
        public DateTime ScheduledFor { get; set; }
        public string StampUserName { get; set; }
    }
}
