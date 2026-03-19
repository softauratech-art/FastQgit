using System;

namespace FastQ.Data.Entities
{
    public class QueueOpenSlot
    {
        public DateTime TheDate { get; set; }
        public long QueueId { get; set; }
        public string SlotBegin { get; set; }
        public string SlotEnd { get; set; }
        public string WeeklySchedule { get; set; }
        public string IntervalTime { get; set; }
        public int AvailableResources { get; set; }
    }
}
