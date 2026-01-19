using System;

namespace FastQ.Data.Entities
{
    public class Customer
    {
        public Guid Id { get; set; }
        public string Phone { get; set; } // required
        public string Name { get; set; }
        public bool SmsOptIn { get; set; }

        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}

