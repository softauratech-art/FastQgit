using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IAppointmentRepository
    {
        Appointment Get(long id);
        void Add(Appointment appointment);
        long AddWalkin(Appointment appointment);
        void Update(Appointment appointment);

        IList<Appointment> ListByQueue(long queueId);
        IList<Appointment> ListByCustomer(long customerId);
        IList<Appointment> ListByEntity(long entityId);
        //IList<Appointment> ListAll();
        IList<QueueOpenSlot> GetQueueOpenSlots(long queueId, DateTime dateLocal);
        IList<ProviderAppointmentData> ListForUser(long entityId, string userId, DateTime rangeStart, DateTime rangeEnd);
        IList<ProviderAppointmentData> ListWalkinsForUser(long entityId, string userId, DateTime rangeStart, DateTime rangeEnd);
        bool ValidatePermitNumber(long queueId, string permitNumber, out string message);
        long? GetQueueIdForSource(char srcType, long sourceId);
    }
}
