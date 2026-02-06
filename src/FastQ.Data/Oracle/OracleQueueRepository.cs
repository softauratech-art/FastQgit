using System;
using System.Collections.Generic;
using System.Data;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Data.Oracle
{
    public sealed class OracleQueueRepository : IQueueRepository
    {
        private readonly string _connectionString;

        public OracleQueueRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Queue Get(long id)
        {
            if (id <= 0) return null;

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateStoredProc(conn, "FQ_PROCS_GET.GET_QUEUE_DETAILS"))
            {
                OracleDb.AddParam(cmd, "p_queueid", id, DbType.Int64);
                OracleDb.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapQueue(reader) : null;
                }
            }
        }

        public void Add(Queue queue)
        {
            using (var conn = OracleDb.Open(_connectionString))
            {
                var queueId = queue.Id;
                if (queueId <= 0)
                {
                    queueId = OracleDb.NextVal(conn, "QUEUESEQ");
                    queue.Id = queueId;
                }

                var locationId = queue.LocationId;
                if (locationId <= 0)
                    throw new InvalidOperationException("LocationId must be a numeric ID.");

                using (var cmd = OracleDb.CreateCommand(conn,
                    @"INSERT INTO VALIDQUEUES
                        (QUEUE_ID, NAME, NAME_ES, NAME_CP, LOCATION_ID, ACTIVEFLAG, EMP_ONLY, HIDE_IN_KIOSK, HIDE_IN_MONITOR, LEAD_TIME_MIN, LEAD_TIME_MAX, HAS_GUIDELINES, HAS_UPLOADS)
                      VALUES
                        (:queueId, :name, :nameEs, :nameCp, :locationId, :activeFlag, :empOnly, :hideInKiosk, :hideInMonitor, :leadMin, :leadMax, :hasGuidelines, :hasUploads)"))
                {
                    var activeFlag = queue.ActiveFlag ? "Y" : "N";
                    OracleDb.AddParam(cmd, "queueId", queueId, DbType.Int64);
                    OracleDb.AddParam(cmd, "name", queue.Name ?? string.Empty, DbType.String);
                    OracleDb.AddParam(cmd, "nameEs", queue.NameEs ?? queue.Name ?? string.Empty, DbType.String);
                    OracleDb.AddParam(cmd, "nameCp", queue.NameCp ?? queue.Name ?? string.Empty, DbType.String);
                    OracleDb.AddParam(cmd, "locationId", locationId, DbType.Int64);
                    OracleDb.AddParam(cmd, "activeFlag", activeFlag, DbType.String);
                    OracleDb.AddParam(cmd, "empOnly", queue.EmpOnly ? "Y" : "N", DbType.String);
                    OracleDb.AddParam(cmd, "hideInKiosk", queue.HideInKiosk ? "Y" : "N", DbType.String);
                    OracleDb.AddParam(cmd, "hideInMonitor", queue.HideInMonitor ? "Y" : "N", DbType.String);
                    OracleDb.AddParam(cmd, "leadMin", ResolveLeadMin(queue), DbType.String);
                    OracleDb.AddParam(cmd, "leadMax", ResolveLeadMax(queue), DbType.String);
                    OracleDb.AddParam(cmd, "hasGuidelines", queue.HasGuidelines ? "Y" : "N", DbType.String);
                    OracleDb.AddParam(cmd, "hasUploads", queue.HasUploads ? "Y" : "N", DbType.String);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Queue queue)
        {
            var queueId = queue.Id;
            if (queueId <= 0)
                throw new InvalidOperationException("Queue Id must be a numeric ID.");
            var locationId = queue.LocationId;
            if (locationId <= 0)
                throw new InvalidOperationException("LocationId must be a numeric ID.");

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"UPDATE VALIDQUEUES
                  SET NAME = :name,
                      NAME_ES = :nameEs,
                      NAME_CP = :nameCp,
                      LOCATION_ID = :locationId,
                      ACTIVEFLAG = :activeFlag,
                      EMP_ONLY = :empOnly,
                      HIDE_IN_KIOSK = :hideInKiosk,
                      HIDE_IN_MONITOR = :hideInMonitor,
                      LEAD_TIME_MIN = :leadMin,
                      LEAD_TIME_MAX = :leadMax,
                      HAS_GUIDELINES = :hasGuidelines,
                      HAS_UPLOADS = :hasUploads
                  WHERE QUEUE_ID = :queueId"))
            {
                OracleDb.AddParam(cmd, "name", queue.Name ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "nameEs", queue.NameEs ?? queue.Name ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "nameCp", queue.NameCp ?? queue.Name ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "locationId", locationId, DbType.Int64);
                OracleDb.AddParam(cmd, "activeFlag", queue.ActiveFlag ? "Y" : "N", DbType.String);
                OracleDb.AddParam(cmd, "empOnly", queue.EmpOnly ? "Y" : "N", DbType.String);
                OracleDb.AddParam(cmd, "hideInKiosk", queue.HideInKiosk ? "Y" : "N", DbType.String);
                OracleDb.AddParam(cmd, "hideInMonitor", queue.HideInMonitor ? "Y" : "N", DbType.String);
                OracleDb.AddParam(cmd, "leadMin", ResolveLeadMin(queue), DbType.String);
                OracleDb.AddParam(cmd, "leadMax", ResolveLeadMax(queue), DbType.String);
                OracleDb.AddParam(cmd, "hasGuidelines", queue.HasGuidelines ? "Y" : "N", DbType.String);
                OracleDb.AddParam(cmd, "hasUploads", queue.HasUploads ? "Y" : "N", DbType.String);
                OracleDb.AddParam(cmd, "queueId", queueId, DbType.Int64);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Queue> ListByLocation(long locationId)
        {
            if (locationId <= 0) return new List<Queue>();

            var list = new List<Queue>();
            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateStoredProc(conn, "FQ_PROCS_GET.GET_QUEUES"))
            {
                OracleDb.AddParam(cmd, "p_location", locationId, DbType.Int64);
                OracleDb.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapQueue(reader));
                    }
                }
            }

            return list;
        }

        public IList<Queue> ListAll()
        {
            var list = new List<Queue>();
            using (var conn = OracleDb.Open(_connectionString))
            {
                using (var cmd = OracleDb.CreateStoredProc(conn, "FQ_PROCS_GET.GET_QUEUES"))
                {
                    OracleDb.AddParam(cmd, "p_location", null, DbType.Int64);
                    OracleDb.AddOutRefCursor(cmd, "p_ref_cursor");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(MapQueue(reader));
                        }
                    }
                }
            }

            return list;
        }

        private static Queue MapQueue(IDataRecord record)
        {
            var queueId = Convert.ToInt64(record["QUEUE_ID"]);
            var locationId = Convert.ToInt64(record["LOCATION_ID"]);
            var leadMinText = record["LEAD_TIME_MIN"]?.ToString();
            var leadMaxText = record["LEAD_TIME_MAX"]?.ToString();
            var activeFlag = (record["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y";
            var empOnly = (record["EMP_ONLY"]?.ToString() ?? "N") == "Y";
            var hideInKiosk = (record["HIDE_IN_KIOSK"]?.ToString() ?? "N") == "Y";
            var hideInMonitor = (record["HIDE_IN_MONITOR"]?.ToString() ?? "N") == "Y";
            var hasGuidelines = (record["HAS_GUIDELINES"]?.ToString() ?? "N") == "Y";
            var hasUploads = (record["HAS_UPLOADS"]?.ToString() ?? "N") == "Y";

            var queue = new Queue
            {
                Id = queueId,
                LocationId = locationId,
                Name = record["NAME"]?.ToString() ?? string.Empty,
                NameEs = record["NAME_ES"]?.ToString() ?? string.Empty,
                NameCp = record["NAME_CP"]?.ToString() ?? string.Empty,
                LeadTimeMin = leadMinText,
                LeadTimeMax = leadMaxText,
                ActiveFlag = activeFlag,
                EmpOnly = empOnly,
                HideInKiosk = hideInKiosk,
                HideInMonitor = hideInMonitor,
                HasGuidelines = hasGuidelines,
                HasUploads = hasUploads
            };

            if (int.TryParse(leadMinText, out var leadMin)) queue.Config.MinHoursLead = leadMin;
            if (int.TryParse(leadMaxText, out var leadMax)) queue.Config.MaxDaysAhead = leadMax;

            return queue;
        }

        private static string ResolveLeadMin(Queue queue)
        {
            return !string.IsNullOrWhiteSpace(queue.LeadTimeMin)
                ? queue.LeadTimeMin
                : queue.Config.MinHoursLead.ToString();
        }

        private static string ResolveLeadMax(Queue queue)
        {
            return !string.IsNullOrWhiteSpace(queue.LeadTimeMax)
                ? queue.LeadTimeMax
                : queue.Config.MaxDaysAhead.ToString();
        }
    }
}

