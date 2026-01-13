using System;
using System.Collections.Generic;
using FastQ.Domain.Entities;
using FastQ.Infrastructure.Common;

namespace FastQ.Infrastructure.InMemory
{
    // Simple in-memory store (prototype). Swap by implementing repositories against a real DB later.
    public sealed class InMemoryStore
    {
        public static readonly InMemoryStore Instance = new InMemoryStore();
        private bool _seeded;

        public object Sync { get; } = new object();

        public Dictionary<Guid, Location> Locations { get; } = new Dictionary<Guid, Location>();
        public Dictionary<Guid, Queue> Queues { get; } = new Dictionary<Guid, Queue>();
        public Dictionary<Guid, Customer> Customers { get; } = new Dictionary<Guid, Customer>();
        public Dictionary<Guid, Provider> Providers { get; } = new Dictionary<Guid, Provider>();
        public Dictionary<Guid, Appointment> Appointments { get; } = new Dictionary<Guid, Appointment>();

        private long _nextAppointmentId = 1005;
        private long _nextCustomerId = 10105;
        private long _nextQueueId = 10041;
        private long _nextLocationId = 11111112;
        private long _nextProviderId = 44444445;

        private InMemoryStore() { }

        public long NextAppointmentId()
            => _nextAppointmentId++;

        public long NextCustomerId()
            => _nextCustomerId++;

        public long NextQueueId()
            => _nextQueueId++;

        public long NextLocationId()
            => _nextLocationId++;

        public long NextProviderId()
            => _nextProviderId++;

        public void EnsureSeeded()
        {
            lock (Sync)
            {
                if (_seeded) return;

                var loc = new Location
                {
                    Id = IdMapper.FromLong(11111111),
                    Name = "Demo Location",
                    TimeZoneId = "UTC"
                };
                Locations[loc.Id] = loc;

                var q1 = new Queue
                {
                    Id = IdMapper.FromLong(22222222),
                    LocationId = loc.Id,
                    Name = "General Queue",
                    Config = new QueueConfig { MaxUpcomingAppointments = 3, MaxDaysAhead = 30, MinHoursLead = 48 }
                };
                var q2 = new Queue
                {
                    Id = IdMapper.FromLong(33333333),
                    LocationId = loc.Id,
                    Name = "Secondary Queue",
                    Config = new QueueConfig { MaxUpcomingAppointments = 3, MaxDaysAhead = 30, MinHoursLead = 48 }
                };
                Queues[q1.Id] = q1;
                Queues[q2.Id] = q2;

                var provider = new Provider
                {
                    Id = IdMapper.FromLong(44444444),
                    LocationId = loc.Id,
                    Name = "Provider A"
                };
                Providers[provider.Id] = provider;

                _seeded = true;
            }
        }
    }
}
