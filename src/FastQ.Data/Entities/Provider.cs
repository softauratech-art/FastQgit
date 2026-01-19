using System;

namespace FastQ.Data.Entities
{
    public class Provider
    {
        public Guid Id { get; set; }
        public Guid LocationId { get; set; }
        public string Name { get; set; }
    }
}

