using System;
using System.Collections.Generic;
using System.Data;
using FastQ.Domain.Entities;
using FastQ.Domain.Repositories;
using FastQ.Infrastructure.Common;

namespace FastQ.Infrastructure.Oracle
{
    public sealed class OracleQueueRepository : IQueueRepository
    {
        private readonly string _connectionString;

        public OracleQueueRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Queue Get(Guid id)
        {
            if (!IdMapper.TryToLong(id, out var queueId)) return null;

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT QUEUE_ID, NAME, LOCATION_ID, LEAD_TIME_MIN, LEAD_TIME_MAX
                  FROM VALIDQUEUES
                  WHERE QUEUE_ID = :queueId"))
            {
                OracleDb.AddParam(cmd, "queueId", queueId, DbType.Int64);
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
                long queueId;
                if (!IdMapper.TryToLong(queue.Id, out queueId))
                {
                    queueId = OracleDb.NextVal(conn, "QUEUESEQ");
                    queue.Id = IdMapper.FromLong(queueId);
                }

                if (!IdMapper.TryToLong(queue.LocationId, out var locationId))
                    throw new InvalidOperationException("LocationId is not mapped to a numeric ID.");

                using (var cmd = OracleDb.CreateCommand(conn,
                    @"INSERT INTO VALIDQUEUES
                        (QUEUE_ID, NAME, NAME_ES, NAME_CP, LOCATION_ID, ACTIVEFLAG, LEAD_TIME_MIN, LEAD_TIME_MAX)
                      VALUES
                        (:queueId, :name, :nameEs, :nameCp, :locationId, 'Y', :leadMin, :leadMax)"))
                {
                    OracleDb.AddParam(cmd, "queueId", queueId, DbType.Int64);
                    OracleDb.AddParam(cmd, "name", queue.Name ?? string.Empty, DbType.String);
                    OracleDb.AddParam(cmd, "nameEs", queue.Name ?? string.Empty, DbType.String);
                    OracleDb.AddParam(cmd, "nameCp", queue.Name ?? string.Empty, DbType.String);
                    OracleDb.AddParam(cmd, "locationId", locationId, DbType.Int64);
                    OracleDb.AddParam(cmd, "leadMin", queue.Config.MinHoursLead.ToString(), DbType.String);
                    OracleDb.AddParam(cmd, "leadMax", queue.Config.MaxDaysAhead.ToString(), DbType.String);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Queue queue)
        {
            if (!IdMapper.TryToLong(queue.Id, out var queueId))
                throw new InvalidOperationException("Queue Id is not mapped to a numeric ID.");
            if (!IdMapper.TryToLong(queue.LocationId, out var locationId))
                throw new InvalidOperationException("LocationId is not mapped to a numeric ID.");

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"UPDATE VALIDQUEUES
                  SET NAME = :name,
                      NAME_ES = :nameEs,
                      NAME_CP = :nameCp,
                      LOCATION_ID = :locationId,
                      LEAD_TIME_MIN = :leadMin,
                      LEAD_TIME_MAX = :leadMax
                  WHERE QUEUE_ID = :queueId"))
            {
                OracleDb.AddParam(cmd, "name", queue.Name ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "nameEs", queue.Name ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "nameCp", queue.Name ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "locationId", locationId, DbType.Int64);
                OracleDb.AddParam(cmd, "leadMin", queue.Config.MinHoursLead.ToString(), DbType.String);
                OracleDb.AddParam(cmd, "leadMax", queue.Config.MaxDaysAhead.ToString(), DbType.String);
                OracleDb.AddParam(cmd, "queueId", queueId, DbType.Int64);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Queue> ListByLocation(Guid locationId)
        {
            if (!IdMapper.TryToLong(locationId, out var locId)) return new List<Queue>();

            var list = new List<Queue>();
            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT QUEUE_ID, NAME, LOCATION_ID, LEAD_TIME_MIN, LEAD_TIME_MAX
                  FROM VALIDQUEUES
                  WHERE LOCATION_ID = :locationId"))
            {
                OracleDb.AddParam(cmd, "locationId", locId, DbType.Int64);
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
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT QUEUE_ID, NAME, LOCATION_ID, LEAD_TIME_MIN, LEAD_TIME_MAX
                  FROM VALIDQUEUES"))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(MapQueue(reader));
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

            var queue = new Queue
            {
                Id = IdMapper.FromLong(queueId),
                LocationId = IdMapper.FromLong(locationId),
                Name = record["NAME"]?.ToString() ?? string.Empty
            };

            if (int.TryParse(leadMinText, out var leadMin)) queue.Config.MinHoursLead = leadMin;
            if (int.TryParse(leadMaxText, out var leadMax)) queue.Config.MaxDaysAhead = leadMax;

            return queue;
        }
    }
}
