using System;
using System.Collections.Generic;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Web.Services
{
    public class ReportingService
    {
        private readonly IAppointmentRepository _appts;
        private readonly IProviderRepository _providers;
        private readonly IQueueRepository _queues;

        public ReportingService(IAppointmentRepository appts, IProviderRepository providers, IQueueRepository queues)
        {
            _appts = appts;
            _providers = providers;
            _queues = queues;
        }

        public IList<Appointment> ListAppointments(Guid? locationId)
        {
            return locationId.HasValue ? _appts.ListByLocation(locationId.Value) : _appts.ListAll();
        }

        public IList<Provider> ListProviders(Guid? locationId)
        {
            return locationId.HasValue ? _providers.ListByLocation(locationId.Value) : _providers.ListAll();
        }

        public IList<Queue> ListQueues(Guid? locationId)
        {
            return locationId.HasValue ? _queues.ListByLocation(locationId.Value) : _queues.ListAll();
        }
    }
}
