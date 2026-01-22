using System;
using FastQ.Data.Common;
using FastQ.Data.Entities;
using FastQ.Web.Models;

namespace FastQ.Web.Services
{
    public class CustomerControllerService
    {
        private readonly BookingService _booking;
        private readonly SharedService _shared;

        public CustomerControllerService()
            : this(new BookingService(), new SharedService())
        {
        }

        public CustomerControllerService(BookingService booking, SharedService shared)
        {
            _booking = booking;
            _shared = shared;
        }

        public Result<Appointment> BookFirstAvailable(Guid locationId, Guid queueId, string phone, bool smsOptIn, string name)
        {
            return _booking.BookFirstAvailable(locationId, queueId, phone, smsOptIn, name);
        }

        public AppointmentSnapshotDto GetAppointmentSnapshot(Guid appointmentId)
        {
            return _shared.GetAppointmentSnapshot(appointmentId);
        }

        public Result Cancel(Guid appointmentId)
        {
            return _booking.Cancel(appointmentId);
        }
    }
}
