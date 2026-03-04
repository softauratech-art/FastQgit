
using FastQ.Data.Entities;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FastQ.Web.Models.Admin
{
    public class QueueVM
    {
        public long Id { get; set; }
        
        [DisplayName("Entity")]
        public long LocationId { get; set; }

        [Required(ErrorMessage = "Name is required")]

        public string Name { get; set; }
        [Required(ErrorMessage = "Spanish Translation required")]
        [DisplayName("Spanish")] 
        public string NameES { get; set; }

        [Required(ErrorMessage = "Creole Translation is required")]
        [DisplayName("Creole")] 
        public string NameCP { get; set; }

        [DisplayName("Is Active?")]
        public bool ActiveFlag { get; set; }

        [DisplayName("Emp Only?")]
        public bool EmpOnly { get; set; }

        [DisplayName("Hide in kisok?")] 
        public bool HideInKiosk { get; set; }

        [DisplayName("Hide in monitor?")]
        public bool HideInMonitor { get; set; }

        [DisplayName("Has Guidelines?")]
        public bool HasGuidelines { get; set; }

        [DisplayName("Requires Uploads?")]
        public bool HasUploads { get; set; }
        
        [Required(ErrorMessage = "Field is required")]
        [DisplayName("Min. Lead Time")] 
        public string LeadTimeMin { get; set; }

        [Required(ErrorMessage = "Field is required")]
        [DisplayName("Max. Lead Time")]        
        public string LeadTimeMax { get; set; }

        public List<SelectListItem> GetLeadTimes(string fld)
        {
            if (fld != null && fld.Length > 0 && fld.ToLower().Equals("leadtimemin"))
            {
                return [
                        new () { Text = "12 hours", Value = "00 12:00:00" },
                        new () { Text = "23 hours", Value = "00 23:00:00" },
                        new () { Text = "Next Day", Value = "01 00:00:00" },
                        new () { Text = "2 Days", Value = "02 00:00:00" },
                        new () { Text = "5 Days", Value = "05 00:00:00" }
                       ];
            }
            else  //return LeadTimeMax as default
            {
                return [
                        new () {Text = "14 Days", Value="14 00:00:00"  },
                        new () {Text = "30 Days", Value="30 00:00:00"  },
                        new () {Text = "60 Days", Value="60 00:00:00"  },
                        new () {Text = "90 Days", Value="90 00:00:00"  },
                        new () {Text = "180 Days", Value="180 00:00:00"  }
                ];
            }
        }

        public IList<QueueScheduleVM> Schedules { get; set; }
        public IList<QueueServiceVM> Services { get; set; }

        // This property contains the available options
        public List<RefCriteriaItem> AvailableRefCriterias
        {
            get
            {
                return new List<RefCriteriaItem>
                    {
                        new RefCriteriaItem {Name="Address", Value="A", IsChecked=this.SelectedRefCriterias!=null && this.SelectedRefCriterias.Contains("A")},
                        new RefCriteriaItem {Name="Case", Value="C", IsChecked=this.SelectedRefCriterias!=null && this.SelectedRefCriterias.Contains("C")},
                        new RefCriteriaItem { Name = "General", Value = "G", IsChecked = this.SelectedRefCriterias!=null && this.SelectedRefCriterias.Contains("G") },
                        new RefCriteriaItem { Name = "Permit", Value = "P", IsChecked = this.SelectedRefCriterias!=null && this.SelectedRefCriterias.Contains("P") }
                    };
            }
        }

        // This property contains the selected options
        [DisplayName("Reference Data")]
        public string[] SelectedRefCriterias { get; set; }

        // This property contains the available options
        public List<ContactMethodItem> AvailableContactMethods
        {
            get
            {
                List<ContactMethodItem> ret = new List<ContactMethodItem>
                    {
                        new ContactMethodItem {Name="Online Meeting", Value="OM", IsChecked=(this.SelectedContactMethods!=null && this.SelectedContactMethods.Contains("OM"))},
                        new ContactMethodItem {Name="Phone Call", Value="PC", IsChecked=(this.SelectedContactMethods!=null && this.SelectedContactMethods.Contains("PC"))},
                        new ContactMethodItem { Name = "In Person", Value = "IP", IsChecked = (this.SelectedContactMethods!=null && this.SelectedContactMethods.Contains("IP")) }
                    };
                return ret;
            }
        }

        // This property contains the selected options
        [Required(ErrorMessage = "Appointment Type(s) required")]
        [DisplayName("Appointment Types")]
        //public IEnumerable<ContactMethodItem> SelectedContactMethods { get; set; }
        public string[] SelectedContactMethods { get; set; }
    }
    public class RefCriteriaItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsChecked { get; set; } // This property will bind the checkbox state
    }
    public class ContactMethodItem
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool IsChecked { get; set; } // This property will bind the checkbox state

    }
    public class QueueScheduleVM
    {
        public long Id { get; set; }
        [Required(ErrorMessage = "Queue Id is required")]
        public long QueueId { get; set; }

        [Required(ErrorMessage = "Begin Date is required")]
        public DateTime BeginDate { get; set; } = DateTime.Now.Date;//"01-JAN-26"


        [Required(ErrorMessage = "End Date is required")]
        public DateTime EndDate { get; set; } = DateTime.Now.Date.AddYears(1);   //"31-DEC-26"
        [Required(ErrorMessage = "Open Time is required")]
        public string OpenTime { get; set; }     //"+00 11:00:00.000000"

        [Required(ErrorMessage = "Close Time is required")]
        public string CloseTime { get; set; }    //"+00 17:00:00.000000"

        [Required(ErrorMessage = "Duration is required")]
        public string Duration { get; set; }     // "+00 01:00:00.000000"

        [Required(ErrorMessage = "Field is required")]
        public string WeeklySchedule { get; set; }   // "24" 0-6 Sun-Sat

        [Required(ErrorMessage = "Field is required")]
        public int ResourcesAvailable { get; set; } = 1;
        public List<SelectListItem> GetDurations()
        {
            return [
                new() { Text = "15 minutes", Value = "00:15:00" },
                new() { Text = "30 minutes", Value = "00:30:00" },
                new() { Text = "45 minutes", Value = "00:45:00" },
                new() { Text = "1 hour", Value = "01:00:00" },
                new() { Text = "1.5 hour", Value = "01:30:00" }
            ];

        }
        // This property contains the available options
        public List<SelectListItem> AvailableWeekdays
        {
            get
            {
                return new List<SelectListItem>
                    {
                        new SelectListItem { Text="Sun", Value="0", Selected=this.WeeklySchedule!=null && this.WeeklySchedule.Contains("0")},
                        new SelectListItem { Text="Mon", Value="1", Selected=this.WeeklySchedule!=null && this.WeeklySchedule.Contains("1")},
                        new SelectListItem { Text = "Tue", Value = "2", Selected = this.WeeklySchedule!=null && this.WeeklySchedule.Contains("2") },
                        new SelectListItem { Text = "Wed", Value = "3", Selected = this.WeeklySchedule!=null && this.WeeklySchedule.Contains("3") },
                        new SelectListItem { Text = "Thu", Value="4", Selected=this.WeeklySchedule!=null && this.WeeklySchedule.Contains("4")},
                        new SelectListItem { Text="Fri", Value="5", Selected=this.WeeklySchedule!=null && this.WeeklySchedule.Contains("5")},
                        new SelectListItem { Text = "Sat", Value = "6", Selected = this.WeeklySchedule!=null && this.WeeklySchedule.Contains("6") }
                    };
            }
        }

    }
    public class QueueServiceVM
    {
        [Required(ErrorMessage = "Id is required")]
        public long Id { get; set; }

        [Required(ErrorMessage = "QueueId is required")]
        public long QueueId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(100, ErrorMessage = "Maximum 100 characters only")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Spanish Translation is required")]
        [MaxLength(100, ErrorMessage = "Maximum 100 characters only")]
        public string NameES { get; set; }

        [Required(ErrorMessage = "Creole Translation is required")]
        [MaxLength(100, ErrorMessage = "Maximum 100 characters only")]
        public string NameCP { get; set; }

        public bool ActiveFlag { get; set; } = true;
    }
}