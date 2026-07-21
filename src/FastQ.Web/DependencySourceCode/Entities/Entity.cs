using System;

namespace FastQ.Data.Entities
{
    public class Entity
    {
        public long Id { get; set; }
        public string Name { get; set; }       
        public string Address { get; set; }
        public string Phone { get; set; }
        public DateTime? OpensAt { get; set; }
        public DateTime? ClosesAt { get; set; }
        public string Description { get; set; }
        public bool ActiveFlag { get; set; } = true;

        public DateTime? OutageBegin { get; set; }
        public DateTime? OutageEnd { get; set; }
        public string OutageMessage {  get; set; }
        public DateTime? OutageNotifyBegin { get; set; }
    }
}

