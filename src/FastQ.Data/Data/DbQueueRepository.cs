using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace FastQ.Data.Db
{
    public sealed class DbQueueRepository : IQueueRepository
    {
        public DbQueueRepository()
        {
          
        }
        #region Queue Base-record
        public Queue Get(long id)
        {
            if (id <= 0) return null;

            using var conn = DataAccess.Open();
            using var cmd = DataAccess.CreateCommand(conn,
                @"SELECT QUEUE_ID, NAME, NAME_ES, NAME_CP, LOCATION_ID, ACTIVEFLAG, EMP_ONLY, HIDE_IN_KIOSK, HIDE_IN_MONITOR, LEAD_TIME_MIN, LEAD_TIME_MAX, HAS_GUIDELINES, HAS_UPLOADS
                  FROM VALIDQUEUES
                  WHERE QUEUE_ID = :queueId");
            DataAccess.AddParam(cmd, "queueId", id, DbType.Int64);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapQueue(reader) : null;
        }

        public Queue GetQueueDetails(long id)
        {
            if (id <= 0) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS_GET.GET_QUEUE_DETAILS"))
            {
                DataAccess.AddParam(cmd, "p_queueid", id, DbType.Int64);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapQueueDetails(reader) : null;
                }
            }
        }

        public long AddOrUpdateQueue(Entities.Queue oqueue, string stampuser)
        {
            using var conn = DataAccess.Open();
            string sp_name = "FQ_UPSERTQUEUE";
            //*EXAMPLE: exec FQ_UPSERTQSCHEDULE(6,10003,'01/01/2026','12/31/2026','00 13:00:00','00 15:30:00','00 01:00:00','234',1,'preddy01',:p_out );

            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {

                DataAccess.AddParam(cmd, "p_queueId", oqueue.Id, DbType.Int64);
                DataAccess.AddParam(cmd, "p_locationid", oqueue.LocationId, DbType.Int64);
                //DataAccess.AddParam(cmd, "p_datebegin", oqueue.BeginDate.ToShortDateString(), DbType.String);
                //DataAccess.AddParam(cmd, "p_dateend", oqueue.EndDate.ToShortDateString(), DbType.String);
                DataAccess.AddParam(cmd, "p_name", oqueue.Name, DbType.String);
                DataAccess.AddParam(cmd, "p_namees", oqueue.NameEs, DbType.String);
                DataAccess.AddParam(cmd, "p_namecp", oqueue.NameCp, DbType.String);
                DataAccess.AddParam(cmd, "p_activeflag", oqueue.ActiveFlag ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_emponly", oqueue.EmpOnly ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_hasguidelines", oqueue.HasGuidelines ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_hasuploads", oqueue.HasUploads ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_hideinkiosk", oqueue.HideInKiosk ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_hideinmonitor", oqueue.HideInMonitor ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_leadtimemax", oqueue.LeadTimeMax, DbType.String);
                DataAccess.AddParam(cmd, "p_leadtimemin", oqueue.LeadTimeMin, DbType.String);
                DataAccess.AddParam(cmd, "p_contacttypes", oqueue.ContactMethods == null ? "" : String.Join(",", oqueue.ContactMethods), DbType.String);
                DataAccess.AddParam(cmd, "p_refcriterias", oqueue.RefCriterias == null ? "" : String.Join(",", oqueue.RefCriterias), DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_newqueueid", 0, DbType.Int64).Direction = ParameterDirection.Output;
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                var dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (dberr != null) throw new InvalidOperationException("DB Error: " + dberr);

                if (cmd.Parameters["p_newqueueid"].Value == DBNull.Value && oqueue.Id == 0) throw new InvalidOperationException("DB Error: Id not returned");
                if (oqueue.Id == 0)
                    return Convert.ToInt64(cmd.Parameters["p_newqueueid"].Value);
                else
                    return oqueue.Id;
            }
        }

        //public void Add(Queue queue)
        //{
        //    using (var conn = DataAccess.Open())
        //    {
        //        var queueId = queue.Id;
        //        if (queueId <= 0)
        //        {
        //            queueId = DataAccess.NextVal(conn, "QUEUESEQ");
        //            queue.Id = queueId;
        //        }

        //        var locationId = queue.LocationId;
        //        if (locationId <= 0)
        //            throw new InvalidOperationException("LocationId must be a numeric ID.");

        //        using (var cmd = DataAccess.CreateCommand(conn,
        //            @"INSERT INTO VALIDQUEUES
        //                (QUEUE_ID, NAME, NAME_ES, NAME_CP, LOCATION_ID, ACTIVEFLAG, EMP_ONLY, HIDE_IN_KIOSK, HIDE_IN_MONITOR, LEAD_TIME_MIN, LEAD_TIME_MAX, HAS_GUIDELINES, HAS_UPLOADS)
        //              VALUES
        //                (:queueId, :name, :nameEs, :nameCp, :locationId, :activeFlag, :empOnly, :hideInKiosk, :hideInMonitor, :leadMin, :leadMax, :hasGuidelines, :hasUploads)"))
        //        {
        //            var activeFlag = queue.ActiveFlag ? "Y" : "N";
        //            DataAccess.AddParam(cmd, "queueId", queueId, DbType.Int64);
        //            DataAccess.AddParam(cmd, "name", queue.Name ?? string.Empty, DbType.String);
        //            DataAccess.AddParam(cmd, "nameEs", queue.NameEs ?? queue.Name ?? string.Empty, DbType.String);
        //            DataAccess.AddParam(cmd, "nameCp", queue.NameCp ?? queue.Name ?? string.Empty, DbType.String);
        //            DataAccess.AddParam(cmd, "locationId", locationId, DbType.Int64);
        //            DataAccess.AddParam(cmd, "activeFlag", activeFlag, DbType.String);
        //            DataAccess.AddParam(cmd, "empOnly", queue.EmpOnly ? "Y" : "N", DbType.String);
        //            DataAccess.AddParam(cmd, "hideInKiosk", queue.HideInKiosk ? "Y" : "N", DbType.String);
        //            DataAccess.AddParam(cmd, "hideInMonitor", queue.HideInMonitor ? "Y" : "N", DbType.String);
        //            DataAccess.AddParam(cmd, "leadMin", ResolveLeadMin(queue), DbType.String);
        //            DataAccess.AddParam(cmd, "leadMax", ResolveLeadMax(queue), DbType.String);
        //            DataAccess.AddParam(cmd, "hasGuidelines", queue.HasGuidelines ? "Y" : "N", DbType.String);
        //            DataAccess.AddParam(cmd, "hasUploads", queue.HasUploads ? "Y" : "N", DbType.String);
        //            cmd.ExecuteNonQuery();
        //        }
        //    }
        //}

        //public void Update(Queue queue)
        //{
        //    var queueId = queue.Id;
        //    if (queueId <= 0)
        //        throw new InvalidOperationException("Queue Id must be a numeric ID.");
        //    var locationId = queue.LocationId;
        //    if (locationId <= 0)
        //        throw new InvalidOperationException("LocationId must be a numeric ID.");

        //    using (var conn = DataAccess.Open())
        //    using (var cmd = DataAccess.CreateCommand(conn,
        //        @"UPDATE VALIDQUEUES
        //          SET NAME = :name,
        //              NAME_ES = :nameEs,
        //              NAME_CP = :nameCp,
        //              LOCATION_ID = :locationId,
        //              ACTIVEFLAG = :activeFlag,
        //              EMP_ONLY = :empOnly,
        //              HIDE_IN_KIOSK = :hideInKiosk,
        //              HIDE_IN_MONITOR = :hideInMonitor,
        //              LEAD_TIME_MIN = :leadMin,
        //              LEAD_TIME_MAX = :leadMax,
        //              HAS_GUIDELINES = :hasGuidelines,
        //              HAS_UPLOADS = :hasUploads
        //          WHERE QUEUE_ID = :queueId"))
        //    {
        //        DataAccess.AddParam(cmd, "name", queue.Name ?? string.Empty, DbType.String);
        //        DataAccess.AddParam(cmd, "nameEs", queue.NameEs ?? queue.Name ?? string.Empty, DbType.String);
        //        DataAccess.AddParam(cmd, "nameCp", queue.NameCp ?? queue.Name ?? string.Empty, DbType.String);
        //        DataAccess.AddParam(cmd, "locationId", locationId, DbType.Int64);
        //        DataAccess.AddParam(cmd, "activeFlag", queue.ActiveFlag ? "Y" : "N", DbType.String);
        //        DataAccess.AddParam(cmd, "empOnly", queue.EmpOnly ? "Y" : "N", DbType.String);
        //        DataAccess.AddParam(cmd, "hideInKiosk", queue.HideInKiosk ? "Y" : "N", DbType.String);
        //        DataAccess.AddParam(cmd, "hideInMonitor", queue.HideInMonitor ? "Y" : "N", DbType.String);
        //        DataAccess.AddParam(cmd, "leadMin", ResolveLeadMin(queue), DbType.String);
        //        DataAccess.AddParam(cmd, "leadMax", ResolveLeadMax(queue), DbType.String);
        //        DataAccess.AddParam(cmd, "hasGuidelines", queue.HasGuidelines ? "Y" : "N", DbType.String);
        //        DataAccess.AddParam(cmd, "hasUploads", queue.HasUploads ? "Y" : "N", DbType.String);
        //        DataAccess.AddParam(cmd, "queueId", queueId, DbType.Int64);
        //        cmd.ExecuteNonQuery();
        //    }
        //}

        public void Delete(long id, string stampuser)
        {
            using var conn = DataAccess.Open();
            string sp_name = "FQ_PROCS_ADMIN.DELETEQUEUE";
            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {
                DataAccess.AddParam(cmd, "p_scheduleid", id, DbType.Int64);
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                var dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (dberr != null) throw new InvalidOperationException(dberr);
            }
        }

        public IList<Entities.Queue> ListByLocation(long entityid)
        {
            return ListByEntity(entityid, string.Empty);
        }

        public IList<Entities.Queue> ListByEntity(long? entityid, string stampuser)
        {
            if (entityid <= 0) return new List<Entities.Queue>();

            var list = new List<Entities.Queue>();
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS_GET.GET_MYQUEUES"))
            {
                DataAccess.AddParam(cmd, "p_userid", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_location", entityid, DbType.Int64);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
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

        //public IList<Queue> ListAll()
        //{
        //    var list = new List<Queue>();
        //    using (var conn = DataAccess.Open())
        //    {
        //        using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS_GET.GET_QUEUES"))
        //        {
        //            DataAccess.AddParam(cmd, "p_location", null, DbType.Int64);
        //            DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
        //            using (var reader = cmd.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    list.Add(MapQueue(reader));
        //                }
        //            }
        //        }
        //    }

        //    return list;
        //}
        public IList<Tuple<long, string>> ListServicesByQueue(long queueId)
        {
            var list = new List<Tuple<long, string>>();
            if (queueId <= 0)
            {
                return list;
            }

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS_GET.GET_SERVICES"))
            {
                DataAccess.AddParam(cmd, "p_queueid", queueId, DbType.Int64);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var serviceId = Convert.ToInt64(reader["SERVICE_ID"]);
                        var serviceName = reader["SERVICE_NAME"] == DBNull.Value
                            ? string.Empty
                            : reader["SERVICE_NAME"].ToString();
                        list.Add(Tuple.Create(serviceId, serviceName));
                    }
                }
            }

            return list;
        }

        public Tuple<string, string, string> GetQueueDetailsJson(long queueId)
        {
            if (queueId <= 0)
            {
                return null;
            }

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"SELECT Q_SERVICES, Q_SCHEDULES, Q_DETAILS
                  FROM VW_QUEUE_DETAILS_JSON
                  WHERE QUEUE_ID = :queueId"))
            {
                DataAccess.AddParam(cmd, "queueId", queueId, DbType.Int64);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var servicesJson = reader["Q_SERVICES"] == DBNull.Value ? string.Empty : reader["Q_SERVICES"].ToString();
                    var schedulesJson = reader["Q_SCHEDULES"] == DBNull.Value ? string.Empty : reader["Q_SCHEDULES"].ToString();
                    var detailsJson = reader["Q_DETAILS"] == DBNull.Value ? string.Empty : reader["Q_DETAILS"].ToString();
                    return Tuple.Create(servicesJson, schedulesJson, detailsJson);
                }
            }
        }

        private static Entities.Queue MapQueue(IDataRecord record)
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

            //if (int.TryParse(leadMinText, out var leadMin)) queue.Config.MinHoursLead = leadMin;
            //if (int.TryParse(leadMaxText, out var leadMax)) queue.Config.MaxDaysAhead = leadMax;

            return queue;
        }

        private static Entities.Queue MapQueueDetails(IDataRecord record)
        {
            var detailsjson = record["Q_DETAILS"];
            var servicesjson = record["Q_SERVICES"];
            var schedulesjson = record["Q_SCHEDULES"];

            JObject jo = JObject.Parse(detailsjson.ToString());

            JArray contactoptions = !string.IsNullOrEmpty(jo["contactoptions"].ToString()) ? (JArray)jo["contactoptions"] : new JArray();
            string[] selectedcontacts = contactoptions.Values<string>("type_key").ToArray();

            JArray refoptions = !string.IsNullOrEmpty(jo["refoptions"].ToString()) ? (JArray)jo["refoptions"] : new JArray();
            string[] selectedrefs = refoptions.Values<string>("ref_key").ToArray();

            var queue = new Entities.Queue
            {
                Name = jo["name"].ToString(),
                NameCp = jo["name_cp"].ToString(),
                NameEs = jo["name_es"].ToString(),
                Id = Convert.ToInt64(jo["queue_id"].ToString()),
                LocationId = Convert.ToInt64(jo["location_id"].ToString()),
                ActiveFlag = (jo["configOptions"]["activeflag"]?.ToString() ?? "N") == "Y",
                LeadTimeMin = jo["configOptions"]["lead_time_min"].ToString(),
                LeadTimeMax = jo["configOptions"]["lead_time_max"].ToString(),
                EmpOnly = (jo["configOptions"]["emp_only"]?.ToString() ?? "N") == "Y",
                HideInKiosk = (jo["configOptions"]["hide_in_kiosk"]?.ToString() ?? "N") == "Y",
                HideInMonitor = (jo["configOptions"]["hide_in_monitor"]?.ToString() ?? "N") == "Y",
                HasUploads = (jo["configOptions"]["has_uploads"]?.ToString() ?? "N") == "Y",
                HasGuidelines = (jo["configOptions"]["has_guidelines"]?.ToString() ?? "N") == "Y",
                Schedules = MapSchedules(schedulesjson.ToString()),
                Services = MapServices(servicesjson.ToString()),
                RefCriterias = selectedrefs,
                ContactMethods = selectedcontacts
            };

            return queue;
        }

        private static IList<QSchedule> MapSchedules(string data)
        {
            var jdata = JObject.Parse(data);
            if (jdata["schedules"] is not JArray schedules) return null;

            List<QSchedule> items = [];
            foreach (JObject item in schedules.Cast<JObject>())
            {
                // Access values using keys
                var sch = new QSchedule
                {
                    Id = Convert.ToInt64(item.GetValue("schedule_id").ToString()),
                    BeginDate = Convert.ToDateTime(item.GetValue("date_begin")),
                    EndDate = Convert.ToDateTime(item.GetValue("date_end").ToString()),
                    CloseTime = item.GetValue("close_time").ToString(),
                    OpenTime = item.GetValue("open_time").ToString(),
                    Duration = item.GetValue("interval_time").ToString(),
                    ResourcesAvailable = Convert.ToInt16(item.GetValue("available_resources").ToString()),
                    WeeklySchedule = item.GetValue("weekly_sch").ToString()
                };

                items.Add(sch);
            }
            return items;
        }

        private static IList<QService> MapServices(string data)
        {
            var jdata = JObject.Parse(data);
            if (jdata["services"] is not JArray services) return null;

            List<QService> items = [];
            foreach (JObject item in services.Cast<JObject>())
            {
                // Access values using keys
                var svc = new QService
                {
                    Name = item.GetValue("service_name").ToString(),
                    NameCp = item.GetValue("service_name_cp").ToString(),
                    NameEs = item.GetValue("service_name_es").ToString(),
                    Id = Convert.ToInt64(item.GetValue("service_id").ToString()),
                    ActiveFlag = (item.GetValue("activeflag")?.ToString() ?? "Y") == "Y"
                };
                items.Add(svc);
            }
            return items;
        }


        //private static string ResolveLeadMin(Queue queue)
        //{
        //    return !string.IsNullOrWhiteSpace(queue.LeadTimeMin)
        //        ? queue.LeadTimeMin
        //        : queue.Config.MinHoursLead.ToString();
        //}

        //private static string ResolveLeadMax(Queue queue)
        //{
        //    return !string.IsNullOrWhiteSpace(queue.LeadTimeMax)
        //        ? queue.LeadTimeMax
        //        : queue.Config.MaxDaysAhead.ToString();
        //}

        #endregion

        #region QueueService
        public Entities.QService GetQService(long id, string stampuser)
        {
            if (id <= 0) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS_GET.GET_QSERVICE_DETAILS"))
            {
                DataAccess.AddParam(cmd, "p_serviceid", id, DbType.Int64);
                DataAccess.AddParam(cmd, "p_userid", id, DbType.String);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? new QService()
                    {
                        Id = Convert.ToInt64(reader["SERVICE_ID"]),
                        QueueId = Convert.ToInt64(reader["QUEUE_ID"]),
                        Name = reader["SERVICE_NAME"]?.ToString(),
                        NameEs = reader["SERVICE_NAME_ES"]?.ToString(),
                        NameCp = reader["SERVICE_NAME_CP"]?.ToString(),
                        ActiveFlag = (reader["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y"
                    } : null;

                }
            }
        }

        public void AddOrUpdateQService(Entities.QService qsvc, string stampuser)
        {
            using var conn = DataAccess.Open();
            string sp_name = "FQ_PROCS_ADMIN.UPSERTQSERVICE";
            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {
                DataAccess.AddParam(cmd, "p_serviceid", qsvc.Id, DbType.Int64);
                DataAccess.AddParam(cmd, "p_queueId", qsvc.QueueId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_name", qsvc.Name, DbType.String);
                DataAccess.AddParam(cmd, "p_namees", qsvc.NameEs, DbType.String);
                DataAccess.AddParam(cmd, "p_namecp", qsvc.NameCp, DbType.String);
                DataAccess.AddParam(cmd, "p_active", qsvc.ActiveFlag ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                var dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (dberr != null) throw new InvalidOperationException("DB Error: " + dberr);
            }
        }
        public void DeleteQService(long serviceid, string stampuser)
        {
            using var conn = DataAccess.Open();
            string sp_name = "FQ_PROCS_ADMIN.DELETEQSERVICE";
            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {
                DataAccess.AddParam(cmd, "p_serviceid", serviceid, DbType.Int64);
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                var dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (dberr != null) throw new InvalidOperationException(dberr);
            }
        }
        #endregion


        #region QueueSchedule
        public Entities.QSchedule GetQSchedule(long id, string stampuser)
        {
            if (id <= 0) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS_GET.GET_QSCHEDULE_DETAILS"))
            {
                DataAccess.AddParam(cmd, "p_scheduleid", id, DbType.Int64);
                DataAccess.AddParam(cmd, "p_userid", id, DbType.String);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    //SCHEDULE_ID, QUEUE_ID, DATE_BEGIN, DATE_END, OPEN_TIME, CLOSE_TIME, INTERVAL_TIME, WEEKLY_SCH, AVAILABLE_RESOURCES
                    return reader.Read() ? new QSchedule()
                    {
                        Id = Convert.ToInt64(reader["SCHEDULE_ID"]),
                        QueueId = Convert.ToInt64(reader["QUEUE_ID"]),
                        BeginDate = Convert.ToDateTime(reader["DATE_BEGIN"]),
                        EndDate = Convert.ToDateTime(reader["DATE_END"]),
                        OpenTime = reader["OPEN_TIME"].ToString(),
                        CloseTime = reader["CLOSE_TIME"].ToString(),
                        Duration = reader["INTERVAL_TIME"].ToString(),
                        WeeklySchedule = reader["WEEKLY_SCH"].ToString(),
                        ResourcesAvailable = Convert.ToInt32(reader["AVAILABLE_RESOURCES"])
                    } : null;
                }
            }
        }

        public void AddOrUpdateQSchedule(Entities.QSchedule qsch, string stampuser)
        {
            using var conn = DataAccess.Open();
            string sp_name = "FQ_PROCS_ADMIN.UPSERTQSCHEDULE";
            //*EXAMPLE: exec FQ_UPSERTQSCHEDULE(6,10003,'01/01/2026','12/31/2026','00 13:00:00','00 15:30:00','00 01:00:00','234',1,'preddy01',:p_out );

            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {
                DataAccess.AddParam(cmd, "p_scheduleid", qsch.Id, DbType.Int64);
                DataAccess.AddParam(cmd, "p_queueId", qsch.QueueId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_datebegin", qsch.BeginDate.ToShortDateString(), DbType.String);
                DataAccess.AddParam(cmd, "p_dateend", qsch.EndDate.ToShortDateString(), DbType.String);
                DataAccess.AddParam(cmd, "p_opentime", qsch.OpenTime, DbType.String);
                DataAccess.AddParam(cmd, "p_closetime", qsch.CloseTime, DbType.String);
                DataAccess.AddParam(cmd, "p_duration", qsch.Duration, DbType.String);
                DataAccess.AddParam(cmd, "p_weekdays", qsch.WeeklySchedule, DbType.String);
                DataAccess.AddParam(cmd, "p_availres", qsch.ResourcesAvailable, DbType.Int64);
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                var dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (dberr != null) throw new InvalidOperationException("DB Error: " + dberr);
            }
        }
        public void DeleteQSchedule(long scheduleid, string stampuser)
        {
            using var conn = DataAccess.Open();
            string sp_name = "FQ_PROCS_ADMIN.DELETEQSCHEDULE";
            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {
                DataAccess.AddParam(cmd, "p_scheduleid", scheduleid, DbType.Int64);
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                var dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (dberr != null) throw new InvalidOperationException(dberr);
            }
        }
        #endregion
    }
}

