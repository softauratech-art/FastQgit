using System.Collections.Generic;

namespace FastQ.Web.Models
{
    public class ServicePageAccess
    {
        public bool IsAdmin { get; set; }
        public bool CanAddEntries { get; set; }

        public IList<long> ProviderQueueIds { get; set; } = new List<long>();
        public IList<long> QueueAdminQueueIds { get; set; } = new List<long>();
        public IList<long> HostQueueIds { get; set; } = new List<long>();
    }
}
