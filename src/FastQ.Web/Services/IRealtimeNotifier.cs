using System;
using FastQ.Data.Entities;

namespace FastQ.Web.Services
{
    public interface IRealtimeNotifier
    {
        void QueueChanged(long locationId, long queueId, string actorUserId = null);
        void AppointmentChanged(Appointment appointment, string actorUserId = null);
    }

    public sealed class NullRealtimeNotifier : IRealtimeNotifier
    {
        public static readonly NullRealtimeNotifier Instance = new NullRealtimeNotifier();
        private NullRealtimeNotifier() { }

        public void QueueChanged(long locationId, long queueId, string actorUserId = null) { }
        public void AppointmentChanged(Appointment appointment, string actorUserId = null) { }
    }
}

