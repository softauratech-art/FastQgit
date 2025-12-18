namespace FastQ.Domain.Entities
{
    public class QueueConfig
    {
        public int MaxUpcomingAppointments { get; set; } = 3;
        public int MaxDaysAhead { get; set; } = 30;
        public int MinHoursLead { get; set; } = 48;
    }
}
