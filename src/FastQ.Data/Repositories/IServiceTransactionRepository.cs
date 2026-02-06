using System;

namespace FastQ.Data.Repositories
{
    public interface IServiceTransactionRepository
    {
        void SetServiceTransaction(char srcType, long srcId, string action, string stampUser, string notes);
        void SaveServiceInfo(char srcType, long srcId, string webexUrl, string notes, string stampUser);
    }
}
