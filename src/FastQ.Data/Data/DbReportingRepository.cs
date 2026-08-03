using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Data;

namespace FastQ.Data.Db
{
    public sealed class DbReportingRepository : IReportingRepository
    {
        public IList<AverageServiceDurationReport> GetServiceDurations(
            long entityId, DateTime startDate, DateTime endDate, string granularity,
            long? queueId, string userId)
        {
            var list = new List<AverageServiceDurationReport>();
            using (var conn = DataAccess.Open())
            {
                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_REPORTING.GET_AVG_SERVICE_DURATION"))
                {
                    // p_entityid NUMBER, p_startdate IN Date, p_enddate IN Date, p_granularity CHAR,
                    // p_queueid NUMBER, p_userid VARCHAR2, p_cur OUT Ref_Cursor_Types.ref_cursor)                    
                    DataAccess.AddParam(cmd, "p_entityid", entityId, DbType.Int64);
                    DataAccess.AddParam(cmd, "p_startdate", startDate.Date, DbType.DateTime);
                    DataAccess.AddParam(cmd, "p_enddate", endDate.Date, DbType.DateTime);
                    DataAccess.AddParam(cmd, "p_granularity", granularity, DbType.String);
                    DataAccess.AddParam(cmd, "p_queueid", queueId, DbType.Int64);
                    DataAccess.AddParam(cmd, "p_userid", (userId ?? string.Empty).Trim().ToLowerInvariant(), DbType.String);
                    DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AverageServiceDurationReport
                            {
                                Queue_Id =  Convert.ToInt64(reader["QUEUE_ID"].ToString()),
                                Queue_Name = reader["QUEUE_NAME"].ToString(),
                                Avg_Duration_Minutes = (reader["AVG_DURATION_MINUTES"].ToString()) == "" ? 0 :  Convert.ToDecimal(reader["AVG_DURATION_MINUTES"].ToString()),
                                Item_Count = Convert.ToInt64(reader["ITEM_COUNT"].ToString())
                            });
                        }
                    }
                }
            }

            return list;
        }
        public IList<QueueLengthsReport> GetQueueLengths(
            long entityId, DateTime startDate, DateTime endDate, string granularity,
            long? queueId, string srcType, string userId
            )
        {
            var list = new List<QueueLengthsReport>();
            using (var conn = DataAccess.Open())
            {
                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_REPORTING.GET_QUEUE_LENGTHS"))
                {
                    //p_entityid NUMBER, p_startdate IN Date, p_enddate IN Date, p_granularity CHAR,
                    //p_queueid NUMBER, p_srctype CHAR, p_userid VARCHAR2, p_cur OUT Ref_Cursor_Types.ref_cursor                   
                    DataAccess.AddParam(cmd, "p_entityid", entityId, DbType.Int64);
                    DataAccess.AddParam(cmd, "p_startdate", startDate.Date, DbType.DateTime);
                    DataAccess.AddParam(cmd, "p_enddate", endDate.Date, DbType.DateTime);
                    DataAccess.AddParam(cmd, "p_granularity", granularity, DbType.String);
                    DataAccess.AddParam(cmd, "p_queueid", queueId, DbType.Int64);
                    DataAccess.AddParam(cmd, "p_srctype", srcType, DbType.String);
                    DataAccess.AddParam(cmd, "p_userid", (userId ?? string.Empty).Trim().ToLowerInvariant(), DbType.String);
                    DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new QueueLengthsReport
                            {
                                Granularity = reader["granularity_date"].ToString(),
                                Queue_Id = Convert.ToInt64(reader["QUEUE_ID"].ToString()),
                                Queue_Name = reader["NAME"].ToString(),
                                Appointment_Count = Convert.ToInt32(reader["APPOINTMENT_COUNT"].ToString()),
                                Walkin_Count = Convert.ToInt32(reader["WALKIN_COUNT"].ToString()),
                                Total_Visits = Convert.ToInt32(reader["TOTAL_VISITS"].ToString())
                            });
                        }
                    }
                }
            }

            return list;

        }
        
    }
}
