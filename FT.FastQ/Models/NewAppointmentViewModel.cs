using System;
using System.ComponentModel.DataAnnotations;

namespace FT.FastQ.Models
{
    public class NewAppointmentViewModel
    {
        [Display(Name = "Service Provider")]
        public string ServiceProvider { get; set; }

        [Display(Name = "Service Type")]
        public string ServiceType { get; set; }

        [Display(Name = "Appointment Date")]
        public string AppointmentDate { get; set; }

        [Display(Name = "Appointment Time")]
        public string AppointmentTime { get; set; }

        [Display(Name = "Appointment Type")]
        public string AppointmentType { get; set; }

        [Display(Name = "Reference")]
        public string Reference { get; set; }

        [Display(Name = "Enter")]
        public string EnterValue { get; set; }

        [Display(Name = "Comments")]
        public string Comments { get; set; }
    }
}


