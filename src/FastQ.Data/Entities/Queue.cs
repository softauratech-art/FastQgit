using System;

namespace FastQ.Data.Entities
{
    public class Queue
    {
        public Guid Id { get; set; }
        public Guid LocationId { get; set; }
        public string Name { get; set; }
        public string NameEs { get; set; }
        public string NameCp { get; set; }
        public bool ActiveFlag { get; set; } = true;
        public bool EmpOnly { get; set; }
        public bool HideInKiosk { get; set; }
        public bool HideInMonitor { get; set; }
        public bool HasGuidelines { get; set; }
        public bool HasUploads { get; set; }
        public string LeadTimeMin { get; set; }
        public string LeadTimeMax { get; set; }

        public QueueConfig Config { get; set; } = new QueueConfig();
    }
}

