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

        public void SetServiceTransaction(char srcType, long srcId, string action, string stampUser, string notes)
        {
            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateStoredProc(conn, "FT_PROCS.SET_SERVICE_TRANSACTION"))
            {
                OracleDb.AddParam(cmd, "p_src_type", srcType.ToString(), DbType.String);
                OracleDb.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
                OracleDb.AddParam(cmd, "p_action", action, DbType.String);
                OracleDb.AddParam(cmd, "p_stampuser", stampUser, DbType.String);
                OracleDb.AddParam(cmd, "p_notes", notes, DbType.String);

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
    }
}
