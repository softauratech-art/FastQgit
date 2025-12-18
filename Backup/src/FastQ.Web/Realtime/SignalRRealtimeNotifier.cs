using System;
using FastQ.Application.Notifications;
using FastQ.Domain.Entities;
using FastQ.Web.Hubs;
using Microsoft.AspNet.SignalR;

namespace FastQ.Web.Realtime
{
    public class SignalRRealtimeNotifier : IRealtimeNotifier
    {
        private static IHubContext Hub => GlobalHost.ConnectionManager.GetHubContext<QueueHub>();

        public void QueueChanged(Guid locationId, Guid queueId)
        {
            var loc = locationId.ToString();
            var q = queueId.ToString();

            Hub.Clients.Group($"loc:{loc}").queueUpdated(loc, q);
            Hub.Clients.Group($"queue:{q}").queueUpdated(loc, q);
        }

        public void AppointmentChanged(Appointment appointment)
        {
            var apptId = appointment.Id.ToString();
            var loc = appointment.LocationId.ToString();
            var q = appointment.QueueId.ToString();

            Hub.Clients.Group($"appt:{apptId}").appointmentUpdated(apptId, appointment.Status.ToString());
            // location + queue listeners can also choose to react
            Hub.Clients.Group($"loc:{loc}").appointmentUpdated(apptId, appointment.Status.ToString());
            Hub.Clients.Group($"queue:{q}").appointmentUpdated(apptId, appointment.Status.ToString());
        }
    }
}
