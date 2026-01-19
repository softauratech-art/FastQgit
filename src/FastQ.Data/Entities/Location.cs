using System;

namespace FastQ.Data.Entities
{
    public class Location
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TimeZoneId { get; set; } = "UTC";
    }
}

