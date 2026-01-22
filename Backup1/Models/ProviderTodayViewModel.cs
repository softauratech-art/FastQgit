using System.Collections.Generic;

namespace FastQ.Web.Models
{
    public class ProviderTodayViewModel
    {
        public string DateText { get; set; }
        public IList<ProviderAppointmentRow> LiveQueue { get; set; } = new List<ProviderAppointmentRow>();
        public IList<ProviderAppointmentRow> Scheduled { get; set; } = new List<ProviderAppointmentRow>();
    }
}
