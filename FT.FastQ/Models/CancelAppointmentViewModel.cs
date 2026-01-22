using System;

namespace FT.FastQ.Models
{
    public class CancelAppointmentViewModel
    {
        public int AppointmentId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string Service { get; set; }
        public string ContactBy { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public bool IsCancelled { get; set; }
    }
}


