using System;

namespace FastQ.Data.Entities
{
    public class Appointment
    {
        public long Id { get; set; }
        public long EntityId { get; set; }
        public long QueueId { get; set; }
        public long CustomerId { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }
        public string CustomerPhone { get; set; }
        public bool CustomerSmsOptIn { get; set; }
        public string ProviderId { get; set; }

        public long? ServiceId { get; set; }
        public string RefCriteria { get; set; }
        public string RefValue { get; set; }
        public string ContactType { get; set; }
        public string MoreInfo { get; set; }
        public DateTime ApptDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public string ConfirmationCode { get; set; }
        public string MeetingUrl { get; set; }
        public string MeetingUrlHost { get; set; }
        public string LanguagePreference { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string StampUser { get; set; }
        public DateTime StampDate { get; set; }

        //public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

        public DateTime ScheduledFor
        {
            get
            {
                if (StartTime.HasValue)
                {
                    return DateTime.SpecifyKind(ApptDate.Date + StartTime.Value, DateTimeKind.Local);
                }

                return DateTime.SpecifyKind(ApptDate, DateTimeKind.Local);
            }
            set
            {
                var local = DateTime.SpecifyKind(value, DateTimeKind.Local);
                ApptDate = local.Date;
                StartTime = local.TimeOfDay;
            }
        }
    }
}
