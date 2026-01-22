using System;
using FastQ.Data.Entities;

namespace FastQ.Web.Models
{
    public class ProviderAppointmentRow
    {
        public Guid AppointmentId { get; set; }
        public string StartTimeText { get; set; }
        public string StartDateText { get; set; }
        public string QueueName { get; set; }
        public string ServiceType { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string StatusText { get; set; }
        public AppointmentStatus Status { get; set; }
        public string ContactMethod { get; set; }
        public DateTime ScheduledForUtc { get; set; }
    }
}
