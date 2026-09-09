using System;

namespace FastQ.Data.Entities
{
    public class ProviderAppointmentData
    {
        public long AppointmentId { get; set; }
        public long QueueId { get; set; }
        public long ServiceId { get; set; }
        public DateTime ScheduledFor { get; set; }
        public AppointmentStatus Status { get; set; }
        public string QueueName { get; set; }
        public string ServiceName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string ContactType { get; set; }
        public string RefValue { get; set; }
        public string LanguagePreference { get; set; }
        public string MeetingUrl { get; set; }
        public string MeetingUrlHost { get; set; }
        public string Notes { get; set; }
        public string ServiceNotes { get; set; }
        public DateTime? ServiceCheckinTime { get; set; }
        public DateTime? ServiceCheckoutTime { get; set; }
        public DateTime? ServiceStartTime { get; set; }
        public DateTime? ServiceEndTime { get; set; }
        public string ServiceStampUser { get; set; }
        public string StampUser { get; set; }
        public string StampUserName { get; set; }
        public bool SmsOptIn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
