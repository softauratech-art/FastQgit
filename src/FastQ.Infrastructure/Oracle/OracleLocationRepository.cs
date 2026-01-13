using System;
using System.Collections.Generic;
using System.Data;
using FastQ.Domain.Entities;
using FastQ.Domain.Repositories;
using FastQ.Infrastructure.Common;

namespace FastQ.Infrastructure.Oracle
{
    public sealed class OracleLocationRepository : ILocationRepository
    {
        private readonly string _connectionString;

        public OracleLocationRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Location Get(Guid id)
        {
            if (!IdMapper.TryToLong(id, out var locationId)) return null;

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT LOCATION_ID, LOCNAME
                  FROM VALIDLOCATIONS
                  WHERE LOCATION_ID = :locationId"))
            {
                OracleDb.AddParam(cmd, "locationId", locationId, DbType.Int64);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapLocation(reader) : null;
                }
            }
        }

        public void Add(Location location)
        {
            using (var conn = OracleDb.Open(_connectionString))
            {
                long locationId;
                if (!IdMapper.TryToLong(location.Id, out locationId))
                {
                    using (var cmd = OracleDb.CreateCommand(conn, "SELECT NVL(MAX(LOCATION_ID),0) + 1 FROM VALIDLOCATIONS"))
                    {
                        locationId = Convert.ToInt64(cmd.ExecuteScalar());
                    }
                    location.Id = IdMapper.FromLong(locationId);
                }

                using (var cmd = OracleDb.CreateCommand(conn,
                    @"INSERT INTO VALIDLOCATIONS
                        (LOCATION_ID, LOCNAME, ACTIVEFLAG)
                      VALUES
                        (:locationId, :name, 'Y')"))
                {
                    OracleDb.AddParam(cmd, "locationId", locationId, DbType.Int64);
                    OracleDb.AddParam(cmd, "name", location.Name ?? string.Empty, DbType.String);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Location location)
        {
            if (!IdMapper.TryToLong(location.Id, out var locationId))
                throw new InvalidOperationException("Location Id is not mapped to a numeric ID.");

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"UPDATE VALIDLOCATIONS
                  SET LOCNAME = :name
                  WHERE LOCATION_ID = :locationId"))
            {
                OracleDb.AddParam(cmd, "name", location.Name ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "locationId", locationId, DbType.Int64);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Location> ListAll()
        {
            var list = new List<Location>();
            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT LOCATION_ID, LOCNAME
                  FROM VALIDLOCATIONS"))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(MapLocation(reader));
                }
            }

            return list;
        }

        private static Location MapLocation(IDataRecord record)
        {
            var locationId = Convert.ToInt64(record["LOCATION_ID"]);
            return new Location
            {
                Id = IdMapper.FromLong(locationId),
                Name = record["LOCNAME"]?.ToString() ?? string.Empty,
                TimeZoneId = "UTC"
            };
        }
    }
}
