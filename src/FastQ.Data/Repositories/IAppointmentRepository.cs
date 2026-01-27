using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IAppointmentRepository
    {
        Appointment Get(Guid id);
        void Add(Appointment appointment);
        void Update(Appointment appointment);

        IList<Appointment> ListByQueue(Guid queueId);
        IList<Appointment> ListByCustomer(Guid customerId);
        IList<Appointment> ListByLocation(Guid locationId);
        IList<Appointment> ListAll();
        IList<ProviderAppointmentData> ListForUser(string userId, DateTime rangeStartUtc, DateTime rangeEndUtc);
        void UpdateStatus(Guid appointmentId, string status, string stampUser, string notes = null);
    }
}

