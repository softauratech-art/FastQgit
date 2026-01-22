using System;

namespace FastQ.Data.Entities
{
    public class Location
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TimeZoneId { get; set; } = "UTC";
        public string Address { get; set; }
        public string Phone { get; set; }
        public DateTime? OpensAt { get; set; }
        public DateTime? ClosesAt { get; set; }
        public string Description { get; set; }
        public bool ActiveFlag { get; set; } = true;
    }
}

