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

        public DateTime ScheduledForUtc { get; set; }
        public AppointmentStatus Status { get; set; }

        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}

