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

    public class WebexApiSettings
    {
        public string Environment { get; set; }
        public string Access_Token { get; set; }
        public string Refresh_Token { get; set; }
        public string Client_Id { get; set; }
        public string Client_Secret { get; set; }
        public string Base_Url { get; set; }
        public string Refresh_Url { get; set; }
    }

}