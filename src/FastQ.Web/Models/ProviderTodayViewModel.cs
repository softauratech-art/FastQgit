using System.Collections.Generic;

namespace FastQ.Web.Models
{
    public class ProviderTodayViewModel
    {
        public string DateText { get; set; }
        public IList<ProviderAppointmentRow> Walkins { get; set; } = new List<ProviderAppointmentRow>();
        public IList<ProviderAppointmentRow> Appointments { get; set; } = new List<ProviderAppointmentRow>();
    }
}
