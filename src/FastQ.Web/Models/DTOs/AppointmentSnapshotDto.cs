namespace FastQ.Web.Models
{
    public class AppointmentSnapshotDto
    {
        public long AppointmentId { get; set; }
        public long EntityId { get; set; }
        public long QueueId { get; set; }

        public string EntityName { get; set; }
        public string QueueName { get; set; }

        public string Status { get; set; }
        public string ScheduledFor { get; set; }
        public string UpdatedUtc { get; set; }

        public int? PositionInQueue { get; set; }
        public int WaitingCount { get; set; }
    }
}
