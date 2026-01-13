using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Domain.Entities;
using FastQ.Domain.Repositories;
using FastQ.Infrastructure.Common;

namespace FastQ.Infrastructure.InMemory
{
    public class InMemoryAppointmentRepository : IAppointmentRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryAppointmentRepository(InMemoryStore store = null)
        {
            _store = store ?? InMemoryStore.Instance;
            _store.EnsureSeeded();
        }

        public Appointment Get(Guid id)
        {
            lock (_store.Sync)
                return _store.Appointments.TryGetValue(id, out var a) ? a : null;
        }

        public void Add(Appointment appointment)
        {
            lock (_store.Sync)
            {
                if (!IdMapper.TryToLong(appointment.Id, out _))
                {
                    appointment.Id = IdMapper.FromLong(_store.NextAppointmentId());
                }
                _store.Appointments[appointment.Id] = appointment;
            }
        }

        public void Update(Appointment appointment)
        {
            lock (_store.Sync)
                _store.Appointments[appointment.Id] = appointment;
        }

        public IList<Appointment> ListByQueue(Guid queueId)
        {
            lock (_store.Sync)
                return _store.Appointments.Values.Where(a => a.QueueId == queueId).ToList();
        }

        public IList<Appointment> ListByCustomer(Guid customerId)
        {
            lock (_store.Sync)
                return _store.Appointments.Values.Where(a => a.CustomerId == customerId).ToList();
        }

        public IList<Appointment> ListByLocation(Guid locationId)
        {
            lock (_store.Sync)
                return _store.Appointments.Values.Where(a => a.LocationId == locationId).ToList();
        }

        public IList<Appointment> ListAll()
        {
            lock (_store.Sync)
                return _store.Appointments.Values.ToList();
        }
    }
}
