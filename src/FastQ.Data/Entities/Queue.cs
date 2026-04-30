using System;
using System.Collections.Generic;

namespace FastQ.Data.Entities
{
    public class Queue
    {
        public long Id { get; set; }
        public long EntityId { get; set; }
        public string Name { get; set; }
        public string NameEs { get; set; }
        public string NameCp { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public bool ActiveFlag { get; set; } = true;
        public bool EmpOnly { get; set; }
        public bool HideInKiosk { get; set; }
        public bool HideInMonitor { get; set; }
        public bool HasGuidelines { get; set; }
        public bool HasUploads { get; set; }
        public string LeadTimeMin { get; set; }
        public string LeadTimeMax { get; set; }

        public string[] RefCriterias { get; set; }
        public string[] ContactMethods { get; set; }

        public IList<QService> Services { get; set; }
        public IList<QSchedule> Schedules { get; set; }
        public IList<QAccess> UserAccess { get; set; }
    }

    public class QSchedule
    {
        public long Id { get; set; }
        public long QueueId { get; set; }
        public DateTime BeginDate { get; set; }     //"01-JAN-26"
        public DateTime EndDate { get; set; }       //"31-DEC-26"
        public string OpenTime { get; set; }        //"+00 11:00:00.000000"
        public string CloseTime { get; set; }       //"+00 17:00:00.000000"
        public string Duration { get; set; }        // "+00 01:00:00.000000"
        public string WeeklySchedule { get; set; }  // "24"
        public int ResourcesAvailable { get; set; } = 1;
    }

    public class QService
    {
        public long Id { get; set; }
        public long QueueId { get; set; }
        public bool ActiveFlag { get; set; }
        public string Name { get; set; }
        public string NameEs { get; set; }
        public string NameCp { get; set; } = string.Empty;
    }
    public class QAccess
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public bool UserActiveFlag { get; set; }
        public long QueueId { get; set; }
        public long EntityId { get; set; }
        public bool HostFlag { get; set; }
        public bool ProviderFlag { get; set; }
        public bool ReporterFlag { get; set; }
        public bool QueueAdminFlag { get; set; }
        public bool ConfigAdminFlag { get; set; }
    }

}
