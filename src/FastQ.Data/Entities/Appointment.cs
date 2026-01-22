using System;

namespace FastQ.Data.Entities
{
    public class Appointment
    {
        public Guid Id { get; set; }
        public Guid LocationId { get; set; }
        public Guid QueueId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? ProviderId { get; set; }

        public Guid? ServiceId { get; set; }
        public string RefCriteria { get; set; }
        public string RefValue { get; set; }
        public string ContactType { get; set; }
        public string MoreInfo { get; set; }
        public DateTime ApptDateUtc { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public string ConfirmationCode { get; set; }
        public string MeetingUrl { get; set; }
        public string LanguagePreference { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public string StampUser { get; set; }
        public DateTime StampDateUtc { get; set; }

        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }

        public DateTime ScheduledForUtc
        {
            get
            {
                if (StartTime.HasValue)
                {
                    return DateTime.SpecifyKind(ApptDateUtc.Date + StartTime.Value, DateTimeKind.Utc);
                }

                return DateTime.SpecifyKind(ApptDateUtc, DateTimeKind.Utc);
            }
            set
            {
                var utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
                ApptDateUtc = utc.Date;
                StartTime = utc.TimeOfDay;
            }
        }
    }
}

