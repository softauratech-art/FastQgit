using System;

namespace FastQ.Data.Entities
{
    public class QueueLengthsReport
    {
        public string Granularity { get; set; }
        public string Queue_Name { get; set; }
        public long Queue_Id { get; set; }
        public int Appointment_Count { get; set; }
        public int Walkin_Count { get; set; }
        public int Total_Visits { get; set; }

    }
    public class AverageServiceDurationReport
    {
        public string Queue_Name { get; set; }
        public long Queue_Id { get; set; }
        public decimal Avg_Duration_Minutes { get; set; }
        public long Item_Count { get; set; }

    }
}

