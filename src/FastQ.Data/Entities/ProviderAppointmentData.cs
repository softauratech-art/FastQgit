using System;

namespace FastQ.Data.Entities
{
    public class ProviderAppointmentData
    {
        public long AppointmentId { get; set; }
        public DateTime ScheduledForUtc { get; set; }
        public AppointmentStatus Status { get; set; }
        public string QueueName { get; set; }
        public string ServiceName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public bool SmsOptIn { get; set; }
    }
}
