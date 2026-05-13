using System;

namespace FastQ.Data.Entities
{
    public class WebexFastQRecord
    {
        public long SrcId { get; set; }
        public string SrcType { get; set; }
        public string WebexMeetingId { get; set; }
        public DateTime AppointmentDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public string CustomerName { get; set; }
        public string Status { get; set; }
        public string EmailAddress { get; set; }
    }
}