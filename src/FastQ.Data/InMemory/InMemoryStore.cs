using System;
using System.Collections.Generic;
using FastQ.Data.Entities;
using FastQ.Data.Common;

namespace FastQ.Data.InMemory
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

                var now = DateTime.UtcNow;

                var cust1 = new Customer
                {
                    Id = IdMapper.FromLong(NextCustomerId()),
                    Name = "Marie Calendar",
                    Phone = "x2690",
                    SmsOptIn = true,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };
                var cust2 = new Customer
                {
                    Id = IdMapper.FromLong(NextCustomerId()),
                    Name = "Martha Stuart",
                    Phone = "x2020",
                    SmsOptIn = false,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };
                var cust3 = new Customer
                {
                    Id = IdMapper.FromLong(NextCustomerId()),
                    Name = "David Cheng",
                    Phone = "x6723",
                    SmsOptIn = true,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };
                var cust4 = new Customer
                {
                    Id = IdMapper.FromLong(NextCustomerId()),
                    Name = "Arlene Martinez",
                    Phone = "x0221",
                    SmsOptIn = true,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };
                var cust5 = new Customer
                {
                    Id = IdMapper.FromLong(NextCustomerId()),
                    Name = "Carter Harper",
                    Phone = "x7812",
                    SmsOptIn = false,
                    CreatedUtc = now,
                    UpdatedUtc = now
                };

                Customers[cust1.Id] = cust1;
                Customers[cust2.Id] = cust2;
                Customers[cust3.Id] = cust3;
                Customers[cust4.Id] = cust4;
                Customers[cust5.Id] = cust5;

                var today = now.Date;

                Appointment CreateAppt(Queue queue, Customer customer, DateTime whenUtc, AppointmentStatus status)
                {
                    var id = IdMapper.FromLong(NextAppointmentId());
                    return new Appointment
                    {
                        Id = id,
                        LocationId = loc.Id,
                        QueueId = queue.Id,
                        CustomerId = customer.Id,
                        ScheduledForUtc = whenUtc,
                        Status = status,
                        CreatedUtc = now,
                        UpdatedUtc = now
                    };
                }

                var appt1 = CreateAppt(q1, cust1, today.AddHours(7).AddMinutes(50), AppointmentStatus.Arrived);
                var appt2 = CreateAppt(q2, cust2, today.AddHours(7).AddMinutes(52), AppointmentStatus.Scheduled);
                var appt3 = CreateAppt(q2, cust3, today.AddHours(10).AddMinutes(30), AppointmentStatus.Scheduled);
                var appt4 = CreateAppt(q1, cust4, today.AddHours(11), AppointmentStatus.Scheduled);
                var appt5 = CreateAppt(q1, cust5, today.AddDays(1).AddHours(14), AppointmentStatus.Scheduled);

                Appointments[appt1.Id] = appt1;
                Appointments[appt2.Id] = appt2;
                Appointments[appt3.Id] = appt3;
                Appointments[appt4.Id] = appt4;
                Appointments[appt5.Id] = appt5;

                _seeded = true;
            }
        }
    }
}

