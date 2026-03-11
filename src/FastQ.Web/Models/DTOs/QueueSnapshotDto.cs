using System.Collections.Generic;

namespace FastQ.Web.Models
{
    public class AppointmentRowDto
    {
        public long AppointmentId { get; set; }
        public long CustomerId { get; set; }
        public string CustomerPhone { get; set; }
        public string Status { get; set; }
        public string ScheduledForUtc { get; set; }
        public string UpdatedUtc { get; set; }
    }

    public class QueueSnapshotDto
    {
        public long LocationId { get; set; }
        public long QueueId { get; set; }
        public string LocationName { get; set; }
        public string QueueName { get; set; }

        public int WaitingCount { get; set; }
        public int InServiceCount { get; set; }
        public int CompletedCount { get; set; }

        public List<AppointmentRowDto> Waiting { get; set; } = new List<AppointmentRowDto>();
        public List<AppointmentRowDto> InService { get; set; } = new List<AppointmentRowDto>();
        public List<AppointmentRowDto> Done { get; set; } = new List<AppointmentRowDto>();
    }
}


