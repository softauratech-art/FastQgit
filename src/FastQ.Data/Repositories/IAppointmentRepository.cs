using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IAppointmentRepository
    {
        Appointment Get(long id);
        void Add(Appointment appointment);
        void Update(Appointment appointment);

        IList<Appointment> ListByQueue(long queueId);
        IList<Appointment> ListByCustomer(long customerId);
        IList<Appointment> ListByLocation(long locationId);
        IList<Appointment> ListAll();
        IList<ProviderAppointmentData> ListForUser(string userId, DateTime rangeStartUtc, DateTime rangeEndUtc);
        IList<ProviderAppointmentData> ListWalkinsForUser(string userId, DateTime rangeStartUtc, DateTime rangeEndUtc);
        void UpdateStatus(long appointmentId, string status, string stampUser, string notes = null);
    }
}

