using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace FastQ.Data.Db
{
    public sealed class DbEntityRepository : IEntityRepository
    {
        public DbEntityRepository()
        {
        }

        public Entity Get(long id)
        {
            if (id <= 0) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_ENTITY"))
            {
                DataAccess.AddParam(cmd, "p_entityid", id, DbType.Int64);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapEntity(reader) : null;
                }
            }
        }

        public void Update(Entity entity, string stampuser)
        {
            var entityId = entity.Id;
            if (entityId <= 0)
                throw new InvalidOperationException("Entity Id must be a numeric ID.");

            using var conn = DataAccess.Open();
            string sp_name = "fqowner.FQ_PROCS_ADMIN.UPSERT_ENTITY";
            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {
                DataAccess.AddParam(cmd, "p_entityid", entity.Id, DbType.Int64);
                DataAccess.AddParam(cmd, "p_name", entity.Name, DbType.String);
                DataAccess.AddParam(cmd, "p_description", entity.Description, DbType.String);
                DataAccess.AddParam(cmd, "p_address", entity.Address, DbType.String);
                DataAccess.AddParam(cmd, "p_phone", entity.Phone, DbType.String);
                DataAccess.AddParam(cmd, "p_activeflag",  "Y", DbType.String);

                DataAccess.AddParam(cmd, "p_opensAt", entity.OpensAt.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_closesAt", entity.ClosesAt.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "outage_notify_begin", entity.OutageNotifyBegin.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_outagebeginat", entity.OutageBegin.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_outageendat", entity.OutageEnd.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "outage_message", entity.OutageMessage, DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_outappts", 0, DbType.Int32).Direction = ParameterDirection.Output;
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                var dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (dberr != null) throw new InvalidOperationException("DB Error: " + dberr);

                if (Int32.TryParse(cmd.Parameters["p_outappts"].Value.ToString(), out var affectedappts))
                {
                    if (affectedappts > 0) 
                        throw new InvalidOperationException($"Note: There are <strong>{affectedappts} appointments</strong> scheduled during this Outage period.<p>Please contact the customers to service, cancel or reschedule these {affectedappts} appointments.</p>");
                }
            }
        }

        public IList<Entity> ListAll()
        {
            var list = new List<Entity>();
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_ENTITIES_ALL"))
            {                
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {                    
                    while (reader.Read())
                    {
                        list.Add(MapEntity(reader));
                    }
                }
            }
            
            return list;
        }

        private static Entity MapEntity(IDataRecord record)
        {
            var entityId = Convert.ToInt64(record["Entity_ID"]);
            var opensAt = record["OPENS_AT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["OPENS_AT"]);
            var closesAt = record["CLOSES_AT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["CLOSES_AT"]);
            var activeFlag = (record["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y";
            
            return new Entity
            {
                Id = entityId,
                Name = ReadString(record, "ENTITY_NAME"),
                Address = record["ADDRESS"]?.ToString() ?? string.Empty,
                Phone = record["PHONE"]?.ToString() ?? string.Empty,
                OpensAt = opensAt,
                ClosesAt = closesAt,
                Description = record["DESCRIPTION"]?.ToString() ?? string.Empty,
                ActiveFlag = activeFlag,
                OutageNotifyBegin = record["OUTAGE_NOTIFY_BEGIN"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["OUTAGE_NOTIFY_BEGIN"]),
                OutageBegin = record["OUTAGE_BEGINS_AT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["OUTAGE_BEGINS_AT"]),
                OutageEnd = record["OUTAGE_ENDS_AT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["OUTAGE_ENDS_AT"]),
                OutageMessage = record["OUTAGE_MESSAGE"]?.ToString() ?? string.Empty,
            };
        }

        private static long ReadInt64(IDataRecord record, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                for (var i = 0; i < record.FieldCount; i++)
                {
                    if (!string.Equals(record.GetName(i), fieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (record.IsDBNull(i))
                    {
                        return 0;
                    }

                    return Convert.ToInt64(record.GetValue(i));
                }
            }

            return 0;
        }

        private static string ReadString(IDataRecord record, params string[] fieldNames)
        {
            foreach (var fieldName in fieldNames)
            {
                for (var i = 0; i < record.FieldCount; i++)
                {
                    if (!string.Equals(record.GetName(i), fieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (record.IsDBNull(i))
                    {
                        return string.Empty;
                    }

                    return record.GetValue(i)?.ToString() ?? string.Empty;
                }
            }

            return string.Empty;
        }
    }
}
