using System;

namespace FastQ.Domain.Entities
{
    public class Location
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TimeZoneId { get; set; } = "UTC";
    }
}
