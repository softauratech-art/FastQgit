using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace FastQ.Web.Hubs
{
    public class QueueHub : Hub
    {
        public Task JoinLocation(string locationId)
            => Groups.Add(Context.ConnectionId, $"loc:{locationId}");

        public Task JoinQueue(string queueId)
            => Groups.Add(Context.ConnectionId, $"queue:{queueId}");

        public Task JoinAppointment(string appointmentId)
            => Groups.Add(Context.ConnectionId, $"appt:{appointmentId}");
    }
}

