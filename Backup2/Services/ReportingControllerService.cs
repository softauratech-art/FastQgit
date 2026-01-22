using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Web.Services
{
    public class ReportingControllerService
    {
        private readonly ReportingService _reporting;

        public ReportingControllerService()
            : this(new ReportingService())
        {
        }

        public ReportingControllerService(ReportingService reporting)
        {
            _reporting = reporting;
        }

        public IList<Appointment> ListAppointments(Guid? locationId)
        {
            return _reporting.ListAppointments(locationId);
        }

        public IList<Provider> ListProviders(Guid? locationId)
        {
            return _reporting.ListProviders(locationId);
        }

        public IList<Queue> ListQueues(Guid? locationId)
        {
            return _reporting.ListQueues(locationId);
        }
    }
}
