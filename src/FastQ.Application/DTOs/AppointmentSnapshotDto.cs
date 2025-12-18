using System;

namespace FastQ.Application.DTOs
{
    public class AppointmentSnapshotDto
    {
        public Guid AppointmentId { get; set; }
        public Guid LocationId { get; set; }
        public Guid QueueId { get; set; }

        public string LocationName { get; set; }
        public string QueueName { get; set; }

        public string Status { get; set; }
        public string ScheduledForUtc { get; set; }
        public string UpdatedUtc { get; set; }

        public int? PositionInQueue { get; set; }
        public int WaitingCount { get; set; }
    }
}
