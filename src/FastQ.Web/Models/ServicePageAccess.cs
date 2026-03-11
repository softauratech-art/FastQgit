using System.Collections.Generic;

namespace FastQ.Web.Models
{
    public class ServicePageAccess
    {
        public bool IsHost { get; set; }
        public bool IsProvider { get; set; }
        public bool IsReporter { get; set; }
        public bool IsQueueAdmin { get; set; }
        public bool IsAdmin { get; set; }

        public bool CanCheckIn { get; set; }
        public bool CanTransfer { get; set; }
        public bool CanCancel { get; set; }
        public bool CanUpdateInfo { get; set; }
        public bool CanAddEntries { get; set; }
        public bool CanViewReports { get; set; }
        public bool CanAccessAdmin { get; set; }

        public IList<long> ProviderQueueIds { get; set; } = new List<long>();
        public IList<long> QueueAdminQueueIds { get; set; } = new List<long>();
    }
}
