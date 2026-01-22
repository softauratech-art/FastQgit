using System;
using System.Collections.Generic;
using System.Data;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Data.Common;

namespace FastQ.Data.Oracle
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
                @"SELECT LOCATION_ID, LOCNAME, ADDRESS, PHONE, OPENS_AT, CLOSES_AT, DESCRIPTION, ACTIVEFLAG
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
                        (LOCATION_ID, LOCNAME, ADDRESS, PHONE, OPENS_AT, CLOSES_AT, DESCRIPTION, ACTIVEFLAG)
                      VALUES
                        (:locationId, :name, :address, :phone, :opensAt, :closesAt, :description, :activeFlag)"))
                {
                    OracleDb.AddParam(cmd, "locationId", locationId, DbType.Int64);
                    OracleDb.AddParam(cmd, "name", location.Name ?? string.Empty, DbType.String);
                    OracleDb.AddParam(cmd, "address", location.Address ?? string.Empty, DbType.String);
                    OracleDb.AddParam(cmd, "phone", location.Phone ?? string.Empty, DbType.String);
                    OracleDb.AddParam(cmd, "opensAt", location.OpensAt, DbType.DateTime);
                    OracleDb.AddParam(cmd, "closesAt", location.ClosesAt, DbType.DateTime);
                    OracleDb.AddParam(cmd, "description", location.Description ?? string.Empty, DbType.String);
                    OracleDb.AddParam(cmd, "activeFlag", location.ActiveFlag ? "Y" : "N", DbType.String);
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
                  SET LOCNAME = :name,
                      ADDRESS = :address,
                      PHONE = :phone,
                      OPENS_AT = :opensAt,
                      CLOSES_AT = :closesAt,
                      DESCRIPTION = :description,
                      ACTIVEFLAG = :activeFlag
                  WHERE LOCATION_ID = :locationId"))
            {
                OracleDb.AddParam(cmd, "name", location.Name ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "address", location.Address ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "phone", location.Phone ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "opensAt", location.OpensAt, DbType.DateTime);
                OracleDb.AddParam(cmd, "closesAt", location.ClosesAt, DbType.DateTime);
                OracleDb.AddParam(cmd, "description", location.Description ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "activeFlag", location.ActiveFlag ? "Y" : "N", DbType.String);
                OracleDb.AddParam(cmd, "locationId", locationId, DbType.Int64);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Location> ListAll()
        {
            var list = new List<Location>();
            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT LOCATION_ID, LOCNAME, ADDRESS, PHONE, OPENS_AT, CLOSES_AT, DESCRIPTION, ACTIVEFLAG
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
            var opensAt = record["OPENS_AT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["OPENS_AT"]);
            var closesAt = record["CLOSES_AT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["CLOSES_AT"]);
            var activeFlag = (record["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y";
            return new Location
            {
                Id = IdMapper.FromLong(locationId),
                Name = record["LOCNAME"]?.ToString() ?? string.Empty,
                Address = record["ADDRESS"]?.ToString() ?? string.Empty,
                Phone = record["PHONE"]?.ToString() ?? string.Empty,
                OpensAt = opensAt,
                ClosesAt = closesAt,
                Description = record["DESCRIPTION"]?.ToString() ?? string.Empty,
                ActiveFlag = activeFlag,
                TimeZoneId = "UTC"
            };
        }
    }
}

