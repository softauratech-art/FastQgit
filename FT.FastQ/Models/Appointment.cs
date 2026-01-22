using System;

namespace FT.FastQ.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public string Queue { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


