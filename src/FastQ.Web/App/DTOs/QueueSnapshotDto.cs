using System;
using System.Collections.Generic;

namespace FastQ.Web.DTOs
{
    public class AppointmentRowDto
    {
        public Guid AppointmentId { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerPhone { get; set; }
        public string Status { get; set; }
        public string ScheduledForUtc { get; set; }
        public string UpdatedUtc { get; set; }
    }

    public class QueueSnapshotDto
    {
        public Guid LocationId { get; set; }
        public Guid QueueId { get; set; }
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

