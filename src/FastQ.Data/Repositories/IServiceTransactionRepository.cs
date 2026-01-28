using System;

namespace FastQ.Data.Repositories
{
    public interface IServiceTransactionRepository
    {
        void SaveServiceInfo(char srcType, long srcId, long? queueId, long? serviceId, string status, string webexUrl, string notes, string stampUser);
    }
}
