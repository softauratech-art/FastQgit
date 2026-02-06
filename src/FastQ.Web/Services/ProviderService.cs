using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Common;
using FastQ.Data.Entities;
using FastQ.Data.Oracle;
using FastQ.Data.Repositories;
using FastQ.Web.Helpers;
using FastQ.Web.Models;

namespace FastQ.Web.Services
{
    public class ProviderService
    {
        private readonly IAppointmentRepository _appts;
        private readonly ICustomerRepository _customers;
        private readonly IQueueRepository _queues;
        private readonly ILocationRepository _locations;
        private readonly IServiceTransactionRepository _serviceTransactions;
        private readonly IClock _clock;
        private readonly IRealtimeNotifier _rt;

        public ProviderService()
            : this(
                OracleRepositoryFactory.CreateAppointmentRepository(),
                OracleRepositoryFactory.CreateCustomerRepository(),
                OracleRepositoryFactory.CreateQueueRepository(),
                OracleRepositoryFactory.CreateLocationRepository(),
                OracleRepositoryFactory.CreateServiceTransactionRepository(),
                new SystemClock(),
                new SignalRRealtimeNotifier())
        {
        }

        public ProviderService(
            IAppointmentRepository appts,
            ICustomerRepository customers,
            IQueueRepository queues,
            ILocationRepository locations,
            IServiceTransactionRepository serviceTransactions,
            IClock clock,
            IRealtimeNotifier rt)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            _locations = locations;
            _serviceTransactions = serviceTransactions;
            _clock = clock;
            _rt = rt ?? NullRealtimeNotifier.Instance;
        }

        public Result SaveServiceInfo(long appointmentId, char srcType, string webexUrl, string notes, string stampUser)
        {
            var srcId = appointmentId;

            var user = string.IsNullOrWhiteSpace(stampUser) ? "web" : stampUser.Trim();
            _serviceTransactions.SaveServiceInfo(srcType, srcId, webexUrl, notes, user);
            return Result.Success();
        }

        public IList<Queue> ListQueues()
        {
            return _queues.ListAll();
        }

        public IList<Customer> ListCustomers()
        {
            return _customers.ListAll();
        }

        public IList<Appointment> ListAppointmentsForDate(DateTime utcDate)
        {
            var date = utcDate.Date;
            return _appts.ListAll()
                .Where(a => a.ScheduledForUtc.Date == date)
                .ToList();
        }

        public IList<ProviderAppointmentRow> BuildRows(
            IList<Appointment> appointments,
            IDictionary<long, Queue> queueMap,
            IDictionary<long, Customer> customerMap)
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
                    StatusText = GetStatusText(a.Status),
                    ContactMethod = contact
                };
            }).OrderBy(r => r.ScheduledForUtc).ToList();
        }

        public IList<ProviderAppointmentRow> BuildRowsForUser(string userId, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<ProviderAppointmentRow>();
            }

            var rangeStart = rangeStartUtc.Date;
            var rangeEnd = rangeEndUtc.Date;
            var rows = _appts.ListForUser(userId, rangeStart, rangeEnd);

            return BuildProviderRows(rows);
        }

        public IList<ProviderAppointmentRow> BuildWalkinsForUser(string userId, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new List<ProviderAppointmentRow>();
            }

            var rangeStart = rangeStartUtc.Date;
            var rangeEnd = rangeEndUtc.Date;
            var rows = _appts.ListWalkinsForUser(userId, rangeStart, rangeEnd);

            return BuildProviderRows(rows);
        }

        private static IList<ProviderAppointmentRow> BuildProviderRows(IList<ProviderAppointmentData> rows)
        {
            return rows.Select(r =>
            {
                var serviceType = !string.IsNullOrWhiteSpace(r.ServiceName)
                    ? r.ServiceName
                    : (!string.IsNullOrWhiteSpace(r.QueueName) ? $"Questions: {r.QueueName}" : "Questions: General");

                return new ProviderAppointmentRow
                {
                    AppointmentId = r.AppointmentId,
                    ScheduledForUtc = r.ScheduledForUtc,
                    StartTimeText = r.ScheduledForUtc.ToString("h:mm tt"),
                    StartDateText = r.ScheduledForUtc.ToString("MMM dd, yyyy"),
                    QueueName = r.QueueName ?? "Unknown Queue",
                    ServiceType = serviceType,
                    CustomerName = string.IsNullOrWhiteSpace(r.CustomerName) ? "Unknown" : r.CustomerName,
                    Phone = string.IsNullOrWhiteSpace(r.CustomerPhone) ? "-" : r.CustomerPhone,
                    Status = r.Status,
                    StatusText = GetStatusText(r.Status),
                    ContactMethod = r.SmsOptIn ? "Online" : "In-Person"
                };
            }).OrderBy(r => r.ScheduledForUtc).ToList();
        }

        private static string GetStatusText(AppointmentStatus status)
        {
            return status switch
            {
                AppointmentStatus.Arrived => "ARRIVED",
                AppointmentStatus.InService => "IN PROGRESS",
                AppointmentStatus.Completed => "DONE",
                AppointmentStatus.Cancelled => "REMOVED",
                _ => status.ToString().ToUpperInvariant()
            };
        }

        public QueueSnapshotDto GetQueueSnapshot(long locationId, long queueId)
        {
            var location = _locations.Get(locationId);
            var queue = _queues.Get(queueId);

            var all = _appts.ListByQueue(queueId)
                .OrderBy(a => a.Status)
                .ThenBy(a => a.CreatedUtc)
                .ToList();

            bool IsWaiting(Appointment a) => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Arrived;
            bool IsInService(Appointment a) => a.Status == AppointmentStatus.InService;
            bool IsDone(Appointment a) => a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.Cancelled || a.Status == AppointmentStatus.ClosedBySystem || a.Status == AppointmentStatus.TransferredOut;

            var waiting = all.Where(IsWaiting).OrderBy(a => a.CreatedUtc).ToList();
            var inService = all.Where(IsInService).OrderBy(a => a.UpdatedUtc).ToList();
            var done = all.Where(IsDone).OrderByDescending(a => a.UpdatedUtc).Take(50).ToList();

            var dto = new QueueSnapshotDto
            {
                LocationId = locationId,
                QueueId = queueId,
                LocationName = location?.Name ?? "Unknown",
                QueueName = queue?.Name ?? "Unknown",
                WaitingCount = waiting.Count,
                InServiceCount = inService.Count,
                CompletedCount = done.Count
            };

            foreach (var a in waiting)
            {
                var c = _customers.Get(a.CustomerId);
                dto.Waiting.Add(new AppointmentRowDto
                {
                    AppointmentId = a.Id,
                    CustomerId = a.CustomerId,
                    CustomerPhone = c?.Phone ?? "",
                    Status = a.Status.ToString(),
                    ScheduledForUtc = a.ScheduledForUtc.ToString("u"),
                    UpdatedUtc = a.UpdatedUtc.ToString("u")
                });
            }

            foreach (var a in inService)
            {
                var c = _customers.Get(a.CustomerId);
                dto.InService.Add(new AppointmentRowDto
                {
                    AppointmentId = a.Id,
                    CustomerId = a.CustomerId,
                    CustomerPhone = c?.Phone ?? "",
                    Status = a.Status.ToString(),
                    ScheduledForUtc = a.ScheduledForUtc.ToString("u"),
                    UpdatedUtc = a.UpdatedUtc.ToString("u")
                });
            }

            foreach (var a in done)
            {
                var c = _customers.Get(a.CustomerId);
                dto.Done.Add(new AppointmentRowDto
                {
                    AppointmentId = a.Id,
                    CustomerId = a.CustomerId,
                    CustomerPhone = c?.Phone ?? "",
                    Status = a.Status.ToString(),
                    ScheduledForUtc = a.ScheduledForUtc.ToString("u"),
                    UpdatedUtc = a.UpdatedUtc.ToString("u")
                });
            }

            return dto;
        }

        public Result HandleProviderAction(string action, char srcType, long appointmentId, string providerId)
        {
            action = (action ?? string.Empty).Trim().ToLowerInvariant();
            return action switch
            {
                "arrive" => QueueCustomer(srcType, appointmentId, providerId),
                "begin" => BeginService(srcType, appointmentId, providerId),
                "end" => EndService(srcType, appointmentId),
                "remove" => RemoveAppointment(srcType, appointmentId, providerId),
                _ => Result.Fail("Unknown action")
            };
        }

        public Result<Appointment> TransferAppointment(long appointmentId, long targetQueueId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return Result<Appointment>.Fail("Appointment not found.");

            var targetQueue = _queues.Get(targetQueueId);
            if (targetQueue == null) return Result<Appointment>.Fail("Target queue not found.");

            if (targetQueue.LocationId != appt.LocationId)
                return Result<Appointment>.Fail("Transfer must be within the same location.");

            if (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.ClosedBySystem)
                return Result<Appointment>.Fail("Cannot transfer a finished appointment.");

            var now = _clock.UtcNow;
            _appts.UpdateStatus(appt.Id, "Transfered", "web");

            appt.Status = AppointmentStatus.TransferredOut;
            appt.UpdatedUtc = now;
            appt.StampDateUtc = now;

            var newAppt = new Appointment
            {
                Id = 0,
                LocationId = appt.LocationId,
                QueueId = targetQueueId,
                CustomerId = appt.CustomerId,
                ScheduledForUtc = _clock.UtcNow,
                Status = AppointmentStatus.Arrived,
                CreatedBy = "web",
                StampUser = "web",
                CreatedOnUtc = _clock.UtcNow,
                StampDateUtc = _clock.UtcNow,
                CreatedUtc = _clock.UtcNow,
                UpdatedUtc = _clock.UtcNow
            };
            _appts.Add(newAppt);

            _rt.AppointmentChanged(appt);
            _rt.QueueChanged(appt.LocationId, appt.QueueId);

            _rt.AppointmentChanged(newAppt);
            _rt.QueueChanged(newAppt.LocationId, newAppt.QueueId);

            return Result<Appointment>.Success(newAppt);
        }

        public int CloseStaleScheduledAppointments(int staleHours)
        {
            var now = _clock.UtcNow;
            var cutoff = now.AddHours(-staleHours);

            var stale = _appts.ListAll()
                .Where(a => a.Status == AppointmentStatus.Scheduled && a.UpdatedUtc <= cutoff)
                .ToList();

            foreach (var a in stale)
            {
                a.Status = AppointmentStatus.ClosedBySystem;
                a.UpdatedUtc = now;
                a.StampDateUtc = now;
                _appts.Update(a);

                _rt.AppointmentChanged(a);
                _rt.QueueChanged(a.LocationId, a.QueueId);
            }

            return stale.Count;
        }

        private Result QueueCustomer(char srcType, long appointmentId, string providerId)
        {
            var upperSrc = char.ToUpperInvariant(srcType);
            Appointment appt = null;
            if (upperSrc == 'A')
            {
                appt = _appts.Get(appointmentId);
                if (appt == null) return Result.Fail("Appointment not found.");
            }

            if (appt != null && (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.ClosedBySystem))
                return Result.Fail("Cannot queue a finished appointment.");

            var now = _clock.UtcNow;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();
            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "CHECKIN", stampUser, null);

            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.Arrived;
                appt.ProviderId = providerId;
                appt.UpdatedUtc = now;
                appt.StampDateUtc = now;

                _rt.AppointmentChanged(appt);
                _rt.QueueChanged(appt.LocationId, appt.QueueId);
            }

            return Result.Success();
        }

        private Result BeginService(char srcType, long appointmentId, string providerId)
        {
            var upperSrc = char.ToUpperInvariant(srcType);
            Appointment appt = null;
            if (upperSrc == 'A')
            {
                appt = _appts.Get(appointmentId);
                if (appt == null) return Result.Fail("Appointment not found.");
            }

            if (appt != null && appt.Status != AppointmentStatus.Arrived && appt.Status != AppointmentStatus.Scheduled)
                return Result.Fail("Appointment must be queued or scheduled to begin service.");

            var now = _clock.UtcNow;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();

            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "START", stampUser, null);
            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.InService;
                appt.ProviderId = providerId;
                appt.UpdatedUtc = now;
                appt.StampDateUtc = now;

                _rt.AppointmentChanged(appt);
                _rt.QueueChanged(appt.LocationId, appt.QueueId);
            }

            return Result.Success();
        }

        private Result EndService(char srcType, long appointmentId)
        {
            var upperSrc = char.ToUpperInvariant(srcType);
            Appointment appt = null;
            if (upperSrc == 'A')
            {
                appt = _appts.Get(appointmentId);
                if (appt == null) return Result.Fail("Appointment not found.");
            }

            if (appt != null && appt.Status != AppointmentStatus.InService)
                return Result.Fail("Appointment must be in service to end service.");

            var now = _clock.UtcNow;
            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "END", "web", null);
            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.Completed;
                appt.UpdatedUtc = now;
                appt.StampDateUtc = now;

                _rt.AppointmentChanged(appt);
                _rt.QueueChanged(appt.LocationId, appt.QueueId);
            }

            return Result.Success();
        }

        private Result RemoveAppointment(char srcType, long appointmentId, string providerId)
        {
            var upperSrc = char.ToUpperInvariant(srcType);
            Appointment appt = null;
            if (upperSrc == 'A')
            {
                appt = _appts.Get(appointmentId);
                if (appt == null) return Result.Fail("Appointment not found.");
            }

            if (appt != null && (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Cancelled || appt.Status == AppointmentStatus.ClosedBySystem))
                return Result.Fail("Appointment is already finished.");

            var now = _clock.UtcNow;
            var stampUser = string.IsNullOrWhiteSpace(providerId) ? "web" : providerId.Trim();

            _serviceTransactions.SetServiceTransaction(srcType, appointmentId, "REMOVE", stampUser, null);
            if (upperSrc == 'A' && appt != null)
            {
                appt.Status = AppointmentStatus.Cancelled;
                appt.ProviderId = providerId;
                appt.UpdatedUtc = now;
                appt.StampDateUtc = now;

                _rt.AppointmentChanged(appt);
                _rt.QueueChanged(appt.LocationId, appt.QueueId);
            }

            return Result.Success();
        }
    }
}
