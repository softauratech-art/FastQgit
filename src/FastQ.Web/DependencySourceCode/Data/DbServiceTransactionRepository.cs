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

        //public void SetServiceTransaction(char srcType, long srcId, string action, string stampUser, string servicenotes, TimeSpan? endTimeLocal)
        public void SetServiceTransaction(char srcType, long srcId, string action, string stampUser, string servicenotes)
        {
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS.SET_SERVICE_TRANSACTION"))
            {
                DataAccess.AddParam(cmd, "p_src_type", srcType.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_action", action, DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampUser, DbType.String);
                DataAccess.AddParam(cmd, "p_servicenotes", servicenotes, DbType.String);

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

                //if (endTimeLocal.HasValue)
                //{
                //    UpdateSourceEndTime(conn, srcType, srcId, endTimeLocal.Value);
                //}
            }
        }

        //private static void UpdateSourceEndTime(System.Data.Common.DbConnection conn, char srcType, long srcId, TimeSpan endTimeLocal)
        //{
        //    var upperSrc = char.ToUpperInvariant(srcType);
        //    var targetTable = upperSrc == 'W' ? "WALKINS" : "APPOINTMENTS";
        //    var idColumn = upperSrc == 'W' ? "WALKIN_ID" : "APPOINTMENT_ID";

        //    using (var cmd = DataAccess.CreateCommand(conn, $"UPDATE {targetTable} SET END_TIME = TO_DSINTERVAL(:p_end_time) WHERE {idColumn} = :p_src_id"))
        //    {
        //        DataAccess.AddParam(cmd, "p_end_time", ToOracleInterval(endTimeLocal), DbType.String);
        //        DataAccess.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
        //        cmd.ExecuteNonQuery();
        //    }
        //}

        //private static string ToOracleInterval(TimeSpan value)
        //{
        //    var absolute = value.Duration();
        //    var sign = value < TimeSpan.Zero ? "-" : "+";
        //    return string.Format(
        //        System.Globalization.CultureInfo.InvariantCulture,
        //        "{0}{1:00} {2:00}:{3:00}:{4:00}.000000",
        //        sign,
        //        absolute.Days,
        //        absolute.Hours,
        //        absolute.Minutes,
        //        absolute.Seconds);
        //}

        public void SaveServiceInfo(char srcType, long srcId, string guestUrl, string hostUrl, string notes, string stampUser)
        {
            using (var conn = DataAccess.Open())
            {
                var normalizedGuestUrl = NormalizeNullable(guestUrl);
                var normalizedHostUrl = NormalizeNullable(hostUrl);
                
                var resolvedStampUser = string.IsNullOrWhiteSpace(stampUser) ? "fastq" : stampUser.Trim();

                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS.SAVE_SERVICE_INFO"))
                {
                    DataAccess.AddParam(cmd, "p_src_type", srcType.ToString(), DbType.String);
                    DataAccess.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
                    DataAccess.AddParam(cmd, "p_guest_url", normalizedGuestUrl, DbType.String);
                    DataAccess.AddParam(cmd, "p_host_url", normalizedHostUrl, DbType.String);
                    DataAccess.AddParam(cmd, "p_notes", notes, DbType.String);
                    DataAccess.AddParam(cmd, "p_stampuser", resolvedStampUser, DbType.String);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static string NormalizeNullable(string value)
        {
            var trimmed = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        //public long TransferSource(char srcType, long srcId, long targetQueueId, long? targetServiceId, char targetKind, DateTime? targetDate, DateTime? targeEndDate, string refValue, string notes, string stampUser, string sourceAction)
        public long TransferSource(char srcType, long srcId, long targetQueueId, long? targetServiceId, char targetKind, DateTime? targetDate, DateTime? targeEndDate, string targetNotes, string serviceNotes, string stampUser, string sourceAction)
        {
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS.TRANSFER_SOURCE"))
            {
                DataAccess.AddParam(cmd, "p_src_type", srcType.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_target_queue_id", targetQueueId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_target_service_id", targetServiceId.HasValue ? (object)targetServiceId.Value : DBNull.Value, DbType.Int64);
                DataAccess.AddParam(cmd, "p_target_kind", targetKind.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_target_date", targetDate.HasValue ? (object)targetDate.Value : DBNull.Value, DbType.DateTime);
                DataAccess.AddParam(cmd, "p_target_enddate", targeEndDate.HasValue ? (object)targeEndDate.Value : DBNull.Value, DbType.DateTime); 
                //DataAccess.AddParam(cmd, "p_ref_value", refValue, DbType.String);
                DataAccess.AddParam(cmd, "p_target_notes", targetNotes, DbType.String);
                DataAccess.AddParam(cmd, "p_servicenotes", serviceNotes, DbType.String);

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

                DataAccess.AddParam(cmd, "p_source_action", sourceAction, DbType.String);

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

        public long CloseAndAddSource(char srcType, long srcId, bool additionalService, long? targetQueueId, long? targetServiceId, char? targetKind, DateTime? targetDate, DateTime? targeEndDate, string notes, string servicenotes, string stampUser)
        {
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS.CLOSE_AND_ADD_SOURCE"))
            {
                DataAccess.AddParam(cmd, "p_src_type", srcType.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_src_id", srcId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_additional", additionalService ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_target_queue_id", targetQueueId.HasValue ? (object)targetQueueId.Value : DBNull.Value, DbType.Int64);
                DataAccess.AddParam(cmd, "p_target_service_id", targetServiceId.HasValue ? (object)targetServiceId.Value : DBNull.Value, DbType.Int64);
                DataAccess.AddParam(cmd, "p_target_kind", targetKind.HasValue ? targetKind.Value.ToString() : (object)DBNull.Value, DbType.String);
                DataAccess.AddParam(cmd, "p_target_date", targetDate.HasValue ? (object)targetDate.Value : DBNull.Value, DbType.DateTime);
                DataAccess.AddParam(cmd, "p_target_enddate", targeEndDate.HasValue ? (object)targeEndDate.Value : DBNull.Value, DbType.DateTime);
                //DataAccess.AddParam(cmd, "p_ref_value", refValue, DbType.String);
                DataAccess.AddParam(cmd, "p_notes", notes, DbType.String);
                DataAccess.AddParam(cmd, "p_servicenotes", servicenotes, DbType.String);
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
