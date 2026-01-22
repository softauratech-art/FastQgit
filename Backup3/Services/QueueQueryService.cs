using System;
using System.Linq;
using FastQ.Web.Models;
using FastQ.Data.Entities;
using FastQ.Data.Oracle;
using FastQ.Data.Repositories;

namespace FastQ.Web.Services
{
    public class QueueQueryService
    {
        private readonly IAppointmentRepository _appts;
        private readonly ICustomerRepository _customers;
        private readonly IQueueRepository _queues;
        private readonly ILocationRepository _locations;

        public QueueQueryService()
            : this(
                OracleRepositoryFactory.CreateAppointmentRepository(),
                OracleRepositoryFactory.CreateCustomerRepository(),
                OracleRepositoryFactory.CreateQueueRepository(),
                OracleRepositoryFactory.CreateLocationRepository())
        {
        }

        public QueueQueryService(IAppointmentRepository appts, ICustomerRepository customers, IQueueRepository queues, ILocationRepository locations)
        {
            _appts = appts;
            _customers = customers;
            _queues = queues;
            _locations = locations;
        }

        public QueueSnapshotDto GetQueueSnapshot(Guid locationId, Guid queueId)
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

        public AppointmentSnapshotDto GetAppointmentSnapshot(Guid appointmentId)
        {
            var appt = _appts.Get(appointmentId);
            if (appt == null) return null;

            var location = _locations.Get(appt.LocationId);
            var queue = _queues.Get(appt.QueueId);

            var snapshot = new AppointmentSnapshotDto
            {
                AppointmentId = appt.Id,
                LocationId = appt.LocationId,
                QueueId = appt.QueueId,
                LocationName = location?.Name ?? "Unknown",
                QueueName = queue?.Name ?? "Unknown",
                Status = appt.Status.ToString(),
                ScheduledForUtc = appt.ScheduledForUtc.ToString("u"),
                UpdatedUtc = appt.UpdatedUtc.ToString("u"),
            };

            // Position in queue (waiting list)
            bool IsWaiting(Appointment a) => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Arrived;

            var waiting = _appts.ListByQueue(appt.QueueId)
                .Where(IsWaiting)
                .OrderBy(a => a.CreatedUtc)
                .ToList();

            snapshot.WaitingCount = waiting.Count;

            var idx = waiting.FindIndex(a => a.Id == appt.Id);
            if (idx >= 0) snapshot.PositionInQueue = idx + 1;

            return snapshot;
        }
    }
}


