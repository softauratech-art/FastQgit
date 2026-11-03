using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Data.Db
{
    public sealed class LobbyEntry
    {
        public long QueueId { get; set; }
        public string QueueName { get; set; }
        public string SourceType { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Time { get; set; }
        internal DateTime? VisitTime { get; set; }
    }

    public sealed class DbLobbyRepository
    {
        private readonly IAppointmentRepository appointments;
        private readonly IQueueRepository queues;
        private readonly IUserRepository users;

        public DbLobbyRepository() : this(DbRepositoryFactory.CreateAppointmentRepository(),
            DbRepositoryFactory.CreateQueueRepository(), DbRepositoryFactory.CreateUserRepository()) { }

        public DbLobbyRepository(IAppointmentRepository appointments, IQueueRepository queues, IUserRepository users)
        {
            this.appointments = appointments;
            this.queues = queues;
            this.users = users;
        }

        public IList<LobbyEntry> Today(string user, long entity)
        {
            return Today(user, entity, DateTime.UtcNow);
        }

        public IList<LobbyEntry> Today(string user, long entity, DateTime utcNow)
        {
            // Refresh through the existing user procedures, rather than trusting cached session roles.
            var account = users.Get(user, "AUTHSERVICE");
            var membership = account?.BusinessEntities?.FirstOrDefault(e => e.EntityId == entity && e.ActiveFlag);
            var allowed = new HashSet<long>((account?.Queues ?? new List<UserQueuePermission>())
                .Where(q => q.EntityId == entity && q.QueueActiveFlag && q.LobbyFlag).Select(q => q.QueueId));
            if (account == null || !account.ActiveFlag || membership == null
                || (!membership.ConfigAdminFlag && allowed.Count == 0))
                throw new UnauthorizedAccessException("Lobby access required");

            var visible = queues.ListByEntity(entity, user)
                .Where(q => q.ActiveFlag && !q.HideInMonitor && (membership.ConfigAdminFlag || allowed.Contains(q.Id)))
                .OrderBy(q => q.Name).ToList();
            var visibleIds = new HashSet<long>(visible.Select(q => q.Id));
            TimeZoneInfo eastern;
            try { eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"); }
            catch (TimeZoneNotFoundException) { eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York"); }
            var today = TimeZoneInfo.ConvertTimeFromUtc(utcNow, eastern).Date;
            var result = new List<LobbyEntry>();

            // Existing GET_MY* procedures take an inclusive end date. Stored values are business wall-clock times.
            AddEntries(result, appointments.ListForUser(entity, user, today, today), "A", today, visibleIds);
            AddEntries(result, appointments.ListWalkinsForUser(entity, user, today, today), "W", today, visibleIds);
            foreach (var queue in visible)
            {
                foreach (var entry in result.Where(e => e.QueueId == queue.Id)) entry.QueueName = queue.Name;
                if (!result.Any(e => e.QueueId == queue.Id))
                    result.Add(new LobbyEntry { QueueId = queue.Id, QueueName = queue.Name });
            }
            return result.OrderBy(e => e.QueueName).ThenBy(e => e.VisitTime).ThenBy(e => e.SourceType).ToList();
        }

        private static void AddEntries(List<LobbyEntry> result, IEnumerable<ProviderAppointmentData> rows,
            string source, DateTime today, HashSet<long> visibleIds)
        {
            foreach (var row in rows.OrderBy(r => source == "W" ? r.CreatedOn ?? r.ScheduledFor : r.ScheduledFor))
            {
                var visitTime = source == "W" ? row.CreatedOn ?? row.ScheduledFor : row.ScheduledFor;
                if (!visibleIds.Contains(row.QueueId) || visitTime.Date != today
                    || (row.Status != AppointmentStatus.Arrived && row.Status != AppointmentStatus.InService)) continue;
                result.Add(new LobbyEntry {
                    QueueId = row.QueueId, SourceType = source, Name = row.CustomerName, VisitTime = visitTime,
                    Status = row.Status == AppointmentStatus.Arrived ? "ARRIVED" : "IN PROGRESS",
                    Time = source == "A" && !row.ScheduledStartTime.HasValue ? "Not scheduled"
                        : visitTime.ToString("hh:mm tt", CultureInfo.InvariantCulture)
                });
            }
        }
    }
}
