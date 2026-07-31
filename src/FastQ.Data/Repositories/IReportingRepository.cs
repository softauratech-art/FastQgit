using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IReportingRepository
    {
        IList<AverageServiceDurationReport> GetServiceDurations(
            long entityId, DateTime startDate, DateTime endDate, string granularity,
            long? queueId, string userId);     
        IList<QueueLengthsReport> GetQueueLengths(
            long entityId, DateTime startDate, DateTime endDate, string granularity,
            long? queueId, string srcType, string userId
            );        
        // granularity Options 'D', 'W', 'M', 'Y'
    }
}

