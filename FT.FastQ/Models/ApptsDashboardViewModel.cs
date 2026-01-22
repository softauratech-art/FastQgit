using System.Collections.Generic;

namespace FT.FastQ.Models
{
    public class ApptsDashboardViewModel
    {
        public List<Appointment> Appointments { get; set; }
        public string ViewType { get; set; } // "Upcoming" or "History"
        public string Email { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public string SortBy { get; set; }
        public string SortDirection { get; set; }

        public ApptsDashboardViewModel()
        {
            Appointments = new List<Appointment>();
            ViewType = "Upcoming";
            CurrentPage = 1;
            PageSize = 10;
            TotalPages = 0;
            TotalRecords = 0;
            SortBy = "AppointmentDateTime";
            SortDirection = "ASC";
        }
    }
}

