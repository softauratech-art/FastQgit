using System;
using System.Linq;
using FastQ.Data.Entities;
using FastQ.Web.Hubs;
using Microsoft.AspNet.SignalR;

namespace FastQ.Web.Services
{
    public class SignalRRealtimeNotifier : IRealtimeNotifier
    {
        private static IHubContext Hub => GlobalHost.ConnectionManager.GetHubContext<QueueHub>();
        private const int IdPreviewLength = 8;

        public void QueueChanged(long entityId, long queueId)
        {
            var entityKey = entityId.ToString();
            var queueKey = queueId.ToString();

            Hub.Clients.Group($"ent:{entityKey}").queueUpdated(entityKey, queueKey);
            Hub.Clients.Group($"queue:{queueKey}").queueUpdated(entityKey, queueKey);
            // Provider dashboard is cross-queue; broadcast to all clients so every board can refresh.
            Hub.Clients.All.queueUpdated(entityKey, queueKey);
        }

        public void AppointmentChanged(Appointment appointment)
        {
            var apptId = appointment.Id.ToString();
            var entityKey = appointment.EntityId.ToString();
            var queueKey = appointment.QueueId.ToString();
            var providerId = appointment.ProviderId?.ToString() ?? string.Empty;

            Hub.Clients.Group($"appt:{apptId}").appointmentUpdated(apptId, appointment.Status.ToString(), providerId);
            // entity + queue listeners can also choose to react
            Hub.Clients.Group($"ent:{entityKey}").appointmentUpdated(apptId, appointment.Status.ToString(), providerId);
            Hub.Clients.Group($"queue:{queueKey}").appointmentUpdated(apptId, appointment.Status.ToString(), providerId);
            // Providers are not always joined to groups; broadcast status as well.
            Hub.Clients.All.appointmentUpdated(apptId, appointment.Status.ToString(), providerId);

            var message = !appointment.SuppressNotification && IsScheduledForToday(appointment)
                ? BuildNotificationMessage(appointment)
                : null;
            if (!string.IsNullOrWhiteSpace(message))
                Hub.Clients.Group($"notify:queue:{queueKey}").notify(message);
        }

        private static bool IsScheduledForToday(Appointment appointment)
        {
            return appointment.ScheduledFor.Date == DateTime.Today;
        }

        private static string BuildNotificationMessage(Appointment appointment)
        {
            var idText = appointment.Id.ToString();
            var shortId = idText.Length <= IdPreviewLength ? idText : idText.Substring(0, IdPreviewLength);
            var customerContext = BuildCustomerContext(appointment);
            switch (appointment.Status)
            {
                case AppointmentStatus.Scheduled:
                    return appointment.IsTransferTarget
                        ? $"Appointment Scheduled ({shortId}{customerContext})."
                        : $"New Booking Created ({shortId}{customerContext}).";
                case AppointmentStatus.Arrived:
                    if (appointment.IsTransferTarget)
                        return $"Customer Arrived ({shortId}{customerContext}).";
                    return appointment.IsNewWalkin
                        ? $"New Walk-In Created ({shortId}{customerContext})."
                        : $"Customer Arrived ({shortId}{customerContext}).";
                case AppointmentStatus.InService:
                    return $"Service Started ({shortId}{customerContext}).";
                case AppointmentStatus.Completed:
                    return $"Service Completed ({shortId}{customerContext}).";
                case AppointmentStatus.Cancelled:
                    return $"Appointment Cancelled ({shortId}{customerContext}).";
                case AppointmentStatus.ClosedBySystem:
                    return $"Appointment Closed By System ({shortId}{customerContext}).";
                case AppointmentStatus.TransferredOut:
                    return $"Appointment Transferred ({shortId}{customerContext}).";
                default:
                    return null;
            }
        }

        private static string BuildCustomerContext(Appointment appointment)
        {
            var fullName = string.Join(" ", new[]
            {
                (appointment.CustomerFirstName ?? string.Empty).Trim(),
                (appointment.CustomerLastName ?? string.Empty).Trim()
            }).Trim();
            var compactName = new string(fullName.Where(char.IsLetterOrDigit).ToArray());

            var digits = new string((appointment.CustomerPhone ?? string.Empty).Where(char.IsDigit).ToArray());
            var phoneLast4 = digits.Length >= 4 ? digits.Substring(digits.Length - 4) : string.Empty;

            if (string.IsNullOrWhiteSpace(compactName) && string.IsNullOrWhiteSpace(phoneLast4))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(compactName))
            {
                return "-" + phoneLast4;
            }

            if (string.IsNullOrWhiteSpace(phoneLast4))
            {
                return "-" + compactName;
            }

            return "-" + compactName + "/" + phoneLast4;
        }
    }
}
