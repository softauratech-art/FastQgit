using System;
using System.Data;
using FastQ.Data.Repositories;

namespace FastQ.Data.Db
{
    public sealed class DbServiceTransactionRepository : IServiceTransactionRepository
    {
        public DbServiceTransactionRepository()
        {
        }

        public void SetServiceTransaction(char srcType, long srcId, string action, string stampUser, string notes)
        {
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS.SET_SERVICE_TRANSACTION"))
            {
                DataAccess.AddParam(cmd, "p_src_type", srcType.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_action", action, DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampUser, DbType.String);
                DataAccess.AddParam(cmd, "p_notes", notes, DbType.String);

                var outMsg = cmd.CreateParameter();
                outMsg.ParameterName = "p_outmsg";
                outMsg.Direction = ParameterDirection.Output;
                outMsg.DbType = DbType.String;
                outMsg.Size = 4000;
                cmd.Parameters.Add(outMsg);

                cmd.ExecuteNonQuery();

                var message = outMsg.Value == DBNull.Value ? string.Empty : outMsg.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                    throw new InvalidOperationException(message);
            }
        }

        public void SaveServiceInfo(char srcType, long srcId, string webexUrl, string notes, string stampUser)
        {
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "FT_PROCS_IDU.SAVE_SERVICE_INFO"))
            {
                DataAccess.AddParam(cmd, "p_src_type", srcType.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_webex_url", webexUrl, DbType.String);
                DataAccess.AddParam(cmd, "p_notes", notes, DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampUser, DbType.String);

                var outMsg = cmd.CreateParameter();
                outMsg.ParameterName = "p_outmsg";
                outMsg.Direction = ParameterDirection.Output;
                outMsg.DbType = DbType.String;
                outMsg.Size = 4000;
                cmd.Parameters.Add(outMsg);

                cmd.ExecuteNonQuery();

                var message = outMsg.Value == DBNull.Value ? string.Empty : outMsg.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                    throw new InvalidOperationException(message);
            }
        }

        public long TransferSource(char srcType, long srcId, long targetQueueId, long? targetServiceId, char targetKind, DateTime? targetDateUtc, string refValue, string notes, string stampUser)
        {
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS.TRANSFER_SOURCE"))
            {
                DataAccess.AddParam(cmd, "p_src_type", srcType.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_target_queue_id", targetQueueId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_target_service_id", targetServiceId.HasValue ? (object)targetServiceId.Value : DBNull.Value, DbType.Int64);
                DataAccess.AddParam(cmd, "p_target_kind", targetKind.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_target_date", targetDateUtc.HasValue ? (object)targetDateUtc.Value.Date : DBNull.Value, DbType.DateTime);
                DataAccess.AddParam(cmd, "p_ref_value", refValue, DbType.String);
                DataAccess.AddParam(cmd, "p_notes", notes, DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampUser, DbType.String);

                var outNewId = cmd.CreateParameter();
                outNewId.ParameterName = "p_new_src_id";
                outNewId.Direction = ParameterDirection.Output;
                outNewId.DbType = DbType.Int64;
                cmd.Parameters.Add(outNewId);

                var outMsg = cmd.CreateParameter();
                outMsg.ParameterName = "p_outmsg";
                outMsg.Direction = ParameterDirection.Output;
                outMsg.DbType = DbType.String;
                outMsg.Size = 4000;
                cmd.Parameters.Add(outMsg);

                cmd.ExecuteNonQuery();

                var message = outMsg.Value == DBNull.Value ? string.Empty : outMsg.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                    throw new InvalidOperationException(message);

                if (outNewId.Value == null || outNewId.Value == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt64(outNewId.Value);
            }
        }

        public long CloseAndAddSource(char srcType, long srcId, bool additionalService, long? targetQueueId, long? targetServiceId, char? targetKind, DateTime? targetDateUtc, string refValue, string notes, string stampUser)
        {
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS.CLOSE_AND_ADD_SOURCE"))
            {
                DataAccess.AddParam(cmd, "p_src_type", srcType.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_additional", additionalService ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_target_queue_id", targetQueueId.HasValue ? (object)targetQueueId.Value : DBNull.Value, DbType.Int64);
                DataAccess.AddParam(cmd, "p_target_service_id", targetServiceId.HasValue ? (object)targetServiceId.Value : DBNull.Value, DbType.Int64);
                DataAccess.AddParam(cmd, "p_target_kind", targetKind.HasValue ? targetKind.Value.ToString() : (object)DBNull.Value, DbType.String);
                DataAccess.AddParam(cmd, "p_target_date", targetDateUtc.HasValue ? (object)targetDateUtc.Value.Date : DBNull.Value, DbType.DateTime);
                DataAccess.AddParam(cmd, "p_ref_value", refValue, DbType.String);
                DataAccess.AddParam(cmd, "p_notes", notes, DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampUser, DbType.String);

                var outNewId = cmd.CreateParameter();
                outNewId.ParameterName = "p_new_src_id";
                outNewId.Direction = ParameterDirection.Output;
                outNewId.DbType = DbType.Int64;
                cmd.Parameters.Add(outNewId);

                var outMsg = cmd.CreateParameter();
                outMsg.ParameterName = "p_outmsg";
                outMsg.Direction = ParameterDirection.Output;
                outMsg.DbType = DbType.String;
                outMsg.Size = 4000;
                cmd.Parameters.Add(outMsg);

                cmd.ExecuteNonQuery();

                var message = outMsg.Value == DBNull.Value ? string.Empty : outMsg.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                    throw new InvalidOperationException(message);

                if (outNewId.Value == null || outNewId.Value == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt64(outNewId.Value);
            }
        }
    }
}
