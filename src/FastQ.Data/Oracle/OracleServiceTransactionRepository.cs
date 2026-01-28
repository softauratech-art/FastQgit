using System;
using System.Data;
using FastQ.Data.Repositories;

namespace FastQ.Data.Oracle
{
    public sealed class OracleServiceTransactionRepository : IServiceTransactionRepository
    {
        private readonly string _connectionString;

        public OracleServiceTransactionRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void SaveServiceInfo(char srcType, long srcId, long? queueId, long? serviceId, string status, string webexUrl, string notes, string stampUser)
        {
            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateStoredProc(conn, "FT_PROCS_IDU.SAVE_SERVICE_INFO"))
            {
                OracleDb.AddParam(cmd, "p_src_type", srcType.ToString(), DbType.String);
                OracleDb.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
                OracleDb.AddParam(cmd, "p_queue_id", queueId.HasValue ? (object)queueId.Value : DBNull.Value, DbType.Int64);
                OracleDb.AddParam(cmd, "p_service_id", serviceId.HasValue ? (object)serviceId.Value : DBNull.Value, DbType.Int64);
                OracleDb.AddParam(cmd, "p_status", status, DbType.String);
                OracleDb.AddParam(cmd, "p_webex_url", webexUrl, DbType.String);
                OracleDb.AddParam(cmd, "p_notes", notes, DbType.String);
                OracleDb.AddParam(cmd, "p_stampuser", stampUser, DbType.String);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
