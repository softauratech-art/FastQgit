namespace FastQ.Web.Models
{
    public class AppointmentSnapshotDto
    {
        public long AppointmentId { get; set; }
        public long LocationId { get; set; }
        public long QueueId { get; set; }

        public string LocationName { get; set; }
        public string QueueName { get; set; }

        public string Status { get; set; }
        public string ScheduledForUtc { get; set; }
        public string UpdatedUtc { get; set; }

        public int? PositionInQueue { get; set; }
        public int WaitingCount { get; set; }
    }
}


