using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Common;
using FastQ.Data.Entities;
using FastQ.Web.Models;

namespace FastQ.Web.Services
{
    public class ProviderControllerService
    {
        private readonly ProviderScheduleService _schedule;
        private readonly ProviderService _provider;
        private readonly TransferService _transfer;
        private readonly SharedService _shared;

        public ProviderControllerService()
            : this(new ProviderScheduleService(), new ProviderService(), new TransferService(), new SharedService())
        {
        }

        public ProviderControllerService(
            ProviderScheduleService schedule,
            ProviderService provider,
            TransferService transfer,
            SharedService shared)
        {
            _schedule = schedule;
            _provider = provider;
            _transfer = transfer;
            _shared = shared;
        }

        public IList<Queue> ListQueues()
        {
            return _schedule.ListQueues();
        }

        public IList<Customer> ListCustomers()
        {
            return _schedule.ListCustomers();
        }

        public IList<Appointment> ListAppointmentsForDate(DateTime utcDate)
        {
            return _schedule.ListAppointmentsForDate(utcDate);
        }

        public QueueSnapshotDto GetQueueSnapshot(Guid locationId, Guid queueId)
        {
            return _shared.GetQueueSnapshot(locationId, queueId);
        }

        public Result HandleProviderAction(string action, Guid appointmentId, Guid providerId)
        {
            action = (action ?? string.Empty).Trim().ToLowerInvariant();
            return action switch
            {
                "arrive" => _provider.MarkArrived(appointmentId),
                "begin" => _provider.BeginService(appointmentId, providerId),
                "end" => _provider.EndService(appointmentId),
                _ => Result.Fail("Unknown action")
            };
        }

        public Result<Appointment> TransferAppointment(Guid appointmentId, Guid targetQueueId)
        {
            return _transfer.Transfer(appointmentId, targetQueueId);
        }

        public int CloseStaleScheduledAppointments(int staleHours)
        {
            return _shared.CloseStaleScheduledAppointments(staleHours);
        }

        public IList<ProviderAppointmentRow> BuildRows(
            IList<Appointment> appointments,
            IDictionary<Guid, Queue> queueMap,
            IDictionary<Guid, Customer> customerMap)
        {
            return appointments.Select(a =>
            {
                queueMap.TryGetValue(a.QueueId, out var queue);
                customerMap.TryGetValue(a.CustomerId, out var customer);

                var contact = customer != null && customer.SmsOptIn ? "Online" : "In-Person";

                return new ProviderAppointmentRow
                {
                    AppointmentId = a.Id,
                    ScheduledForUtc = a.ScheduledForUtc,
                    StartTimeText = a.ScheduledForUtc.ToString("h:mm tt"),
                    StartDateText = a.ScheduledForUtc.ToString("MMM dd, yyyy"),
                    QueueName = queue?.Name ?? "Unknown Queue",
                    ServiceType = queue?.Name != null ? $"Questions: {queue.Name}" : "Questions: General",
                    CustomerName = customer?.Name ?? "Unknown",
                    Phone = customer?.Phone ?? "-",
                    Status = a.Status,
                    StatusText = a.Status.ToString().ToUpperInvariant(),
                    ContactMethod = contact
                };
            }).OrderBy(r => r.ScheduledForUtc).ToList();
        }
    }
}
