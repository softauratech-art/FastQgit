using System;

namespace FastQ.Data.Entities
{
    public class Holiday
    {
        public DateTime Day { get; set; }
        public string Description { get; set; }       
        public bool ActiveFlag { get; set; }
        public string StampUser { get; set; }
        public DateTime StampDate { get; set; }
    }
}