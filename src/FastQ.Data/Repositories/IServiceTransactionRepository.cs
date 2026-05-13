using System;

namespace FastQ.Data.Repositories
{
    public interface IServiceTransactionRepository
    {
        void SetServiceTransaction(char srcType, long srcId, string action, string stampUser, string notes);
        void SaveServiceInfo(char srcType, long srcId, string guestUrl, string hostUrl, string notes, string stampUser);
        long TransferSource(char srcType, long srcId, long targetQueueId, long? targetServiceId, char targetKind, DateTime? targetDate, DateTime? targeEndDate, string targetNotes, string serviceNotes, string stampUser, string sourceAction);
        long CloseAndAddSource(char srcType, long srcId, bool additionalService, long? targetQueueId, long? targetServiceId, char? targetKind, DateTime? targetDate, DateTime? targeEndDate, string targetNotes, string serviceNotes, string stampUser);
    }
}
