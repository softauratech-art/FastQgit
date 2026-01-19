using System;

namespace FastQ.Data.Entities
{
    public class Queue
    {
        public Guid Id { get; set; }
        public Guid LocationId { get; set; }
        public string Name { get; set; }

        public QueueConfig Config { get; set; } = new QueueConfig();
    }
}

