
namespace FastQ.Web.Models.Admin
{
    
using FastQ.Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

    public class QueueVM
    {
        public Int64 Id { get; set; }
        public int LocationId { get; set; }
        public string Name { get; set; }
        public string NameES { get; set; }
        public string NameCP { get; set; }
        public string ActiveFlag { get; set; }
        public string EmpOnly { get; set; }
        public string HideInKiosk { get; set; }
        public string HideInMonitor { get; set; }
        public string HasGuidelines { get; set; }
        public string HasUploads { get; set; }
        public string LeadTimeMin { get; set; }
        public string LeadTimeMax { get; set; }
        public IList<QueueScheduleVM> Schedules { get; set; }
        public IList<QueueServiceVM> Services { get; set; }           
    }

    public class QueueScheduleVM
    {
        public Int64 Id { get; set; }
        public Int64 QueueId { get; set; }
        public DateTime BeginDate { get; set; }  //"01-JAN-26"
        public DateTime EndDate { get; set; }    //"31-DEC-26"
        public string OpenTime { get; set; }     //"+00 11:00:00.000000"
        public string CloseTime { get; set; }    //"+00 17:00:00.000000"
        public string Duration { get; set; }     // "+00 01:00:00.000000"
        public string WeeklySchedule { get; set; }   // "24"
        public int ResourcesAvailable { get; set; } = 1;

        public string WeeklySchPrettyPrint
        {
            get
            {
                IEnumerable<int> digits = WeeklySchedule.Select(c => (int)Char.GetNumericValue(c));
                string ret = string.Empty;

                //
                foreach (int i in digits)
                {
                    //ret += Enum.GetName(typeof(DayOfWeek), i) + " "; //0-based index
                    //switch (i)
                    //{
                    //    case 1: ret += " Sun"; break;
                    //    case 2: ret += " Mon"; break;
                    //    case 3: ret += " Tue"; break;
                    //    case 4: ret += " Wed"; break;
                    //    case 5: ret += " Thu"; break;
                    //    case 6: ret += " Fri"; break;
                    //    case 7: ret += " Sat"; break;
                    //    default: break;
                    //}

                    ret += FastQ.Web.Helpers.Utilities.TranslateToDayOfWeek(i-1, Helpers.Utilities.DayofWeekType.Short);

                }

                return ret;
            }
        }
    }
    public class QueueServiceVM
    {
        public Int64 Id { get; set; }
        public Int64 QueueId { get; set; }
        public string Name { get; set; }
        public string NameES { get; set; }
        public string NameCP { get; set; }
        public bool ActiveFlag { get; set; } = true;
    }
}