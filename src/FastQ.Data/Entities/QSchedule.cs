using System;

namespace FastQ.Data.Entities
{
    public class QSchedule
    {
        public long Id { get; set; }
        public long QueueId { get; set; }
        public DateTime BeginDate { get; set; }     //"01-JAN-26"
        public DateTime EndDate { get; set; }       //"31-DEC-26"
        public string OpenTime { get; set; }        //"+00 11:00:00.000000"
        public string CloseTime { get; set; }       //"+00 17:00:00.000000"
        public string Duration { get; set; }        // "+00 01:00:00.000000"
        public string WeeklySchedule { get; set; }  // "24"
        public int ResourcesAvailable { get; set; } = 1;
    }
}

