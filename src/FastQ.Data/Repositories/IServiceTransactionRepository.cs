using System;

namespace FastQ.Data.Repositories
{
    public interface IServiceTransactionRepository
    {
        void SetServiceTransaction(char srcType, long srcId, string action, string stampUser, string notes);
        void SaveServiceInfo(char srcType, long srcId, string webexUrl, string notes, string stampUser);
        long TransferSource(char srcType, long srcId, long targetQueueId, long? targetServiceId, char targetKind, DateTime? targetDateUtc, string refValue, string notes, string stampUser);
        long CloseAndAddSource(char srcType, long srcId, bool additionalService, long? targetQueueId, long? targetServiceId, char? targetKind, DateTime? targetDateUtc, string refValue, string notes, string stampUser);
    }
}
