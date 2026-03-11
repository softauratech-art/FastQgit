using System;
using FastQ.Web.Services;
using FastQ.Data.Entities;
using FastQ.Web.Hubs;
using Microsoft.AspNet.SignalR;

namespace FastQ.Web.Services
{
    public class SignalRRealtimeNotifier : IRealtimeNotifier
    {
        private static IHubContext Hub => GlobalHost.ConnectionManager.GetHubContext<QueueHub>();
        private const int IdPreviewLength = 8;

        public void QueueChanged(long locationId, long queueId)
        {
            var loc = locationId.ToString();
            var q = queueId.ToString();

            Hub.Clients.Group($"loc:{loc}").queueUpdated(loc, q);
            Hub.Clients.Group($"queue:{q}").queueUpdated(loc, q);
            // Provider dashboard is cross-queue; broadcast to all clients so every board can refresh.
            Hub.Clients.All.queueUpdated(loc, q);
        }

        public void AppointmentChanged(Appointment appointment)
        {
            var apptId = appointment.Id.ToString();
            var loc = appointment.LocationId.ToString();
            var q = appointment.QueueId.ToString();
            var providerId = appointment.ProviderId?.ToString() ?? string.Empty;

            Hub.Clients.Group($"appt:{apptId}").appointmentUpdated(apptId, appointment.Status.ToString(), providerId);
            // location + queue listeners can also choose to react
            Hub.Clients.Group($"loc:{loc}").appointmentUpdated(apptId, appointment.Status.ToString(), providerId);
            Hub.Clients.Group($"queue:{q}").appointmentUpdated(apptId, appointment.Status.ToString(), providerId);
            // Providers are not always joined to groups; broadcast status as well.
            Hub.Clients.All.appointmentUpdated(apptId, appointment.Status.ToString(), providerId);

            var message = BuildNotificationMessage(appointment);
            if (!string.IsNullOrWhiteSpace(message))
                Hub.Clients.All.notify(message);
        }

        private static string BuildNotificationMessage(Appointment appointment)
        {
            var idText = appointment.Id.ToString();
            var shortId = idText.Length <= IdPreviewLength ? idText : idText.Substring(0, IdPreviewLength);
            switch (appointment.Status)
            {
                case AppointmentStatus.Scheduled:
                    return $"New booking created ({shortId}).";
                case AppointmentStatus.Arrived:
                    return $"Customer arrived ({shortId}).";
                case AppointmentStatus.InService:
                    return $"Service started ({shortId}).";
                case AppointmentStatus.Completed:
                    return $"Service completed ({shortId}).";
                case AppointmentStatus.Cancelled:
                    return $"Appointment cancelled ({shortId}).";
                case AppointmentStatus.ClosedBySystem:
                    return $"Appointment closed by system ({shortId}).";
                case AppointmentStatus.TransferredOut:
                    return $"Appointment transferred ({shortId}).";
                default:
                    return null;
            }
        }
    }
}

