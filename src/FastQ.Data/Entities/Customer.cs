using System;

namespace FastQ.Data.Entities
{
    public class Customer
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; } // required
        public bool SmsOptIn { get; set; }
        public bool ActiveFlag { get; set; } = true;
        public string StampUser { get; set; }

        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public DateTime StampDateUtc { get; set; }

        public string Name
        {
            get => $"{FirstName} {LastName}".Trim();
            set
            {
                var trimmed = (value ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    FirstName = string.Empty;
                    LastName = string.Empty;
                    return;
                }

                var parts = trimmed.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                FirstName = parts.Length > 0 ? parts[0] : string.Empty;
                LastName = parts.Length > 1 ? parts[1] : string.Empty;
            }
        }
    }
}

