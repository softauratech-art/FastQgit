using System;

namespace FastQ.Data.Entities
{
    public class Provider
    {
        public Guid Id { get; set; }
        public Guid LocationId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Language { get; set; }
        public bool ActiveFlag { get; set; } = true;
        public bool AdminFlag { get; set; }
        public string Password { get; set; }
        public string Title { get; set; }
        public string StampUser { get; set; }
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

