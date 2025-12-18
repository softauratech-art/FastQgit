using System;

namespace FastQ.Domain.Entities
{
    public class Provider
    {
        public Guid Id { get; set; }
        public Guid LocationId { get; set; }
        public string Name { get; set; }
    }
}
