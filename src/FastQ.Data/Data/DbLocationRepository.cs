using System;
using System.Collections.Generic;
using System.Data;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Data.Db
{
    public sealed class DbLocationRepository : ILocationRepository
    {
        public DbLocationRepository()
        {
        }

        public Location Get(long id)
        {
            if (id <= 0) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"SELECT LOCATION_ID, LOCNAME, ADDRESS, PHONE, OPENS_AT, CLOSES_AT, DESCRIPTION, ACTIVEFLAG
                  FROM VALIDLOCATIONS
                  WHERE LOCATION_ID = :locationId"))
            {
                DataAccess.AddParam(cmd, "locationId", id, DbType.Int64);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapLocation(reader) : null;
                }
            }
        }

        public void Add(Location location)
        {
            using (var conn = DataAccess.Open())
            {
                var locationId = location.Id;
                if (locationId <= 0)
                {
                    using (var cmd = DataAccess.CreateCommand(conn, "SELECT NVL(MAX(LOCATION_ID),0) + 1 FROM VALIDLOCATIONS"))
                    {
                        locationId = Convert.ToInt64(cmd.ExecuteScalar());
                    }
                    location.Id = locationId;
                }

                using (var cmd = DataAccess.CreateCommand(conn,
                    @"INSERT INTO VALIDLOCATIONS
                        (LOCATION_ID, LOCNAME, ADDRESS, PHONE, OPENS_AT, CLOSES_AT, DESCRIPTION, ACTIVEFLAG)
                      VALUES
                        (:locationId, :name, :address, :phone, :opensAt, :closesAt, :description, :activeFlag)"))
                {
                    DataAccess.AddParam(cmd, "locationId", locationId, DbType.Int64);
                    DataAccess.AddParam(cmd, "name", location.Name ?? string.Empty, DbType.String);
                    DataAccess.AddParam(cmd, "address", location.Address ?? string.Empty, DbType.String);
                    DataAccess.AddParam(cmd, "phone", location.Phone ?? string.Empty, DbType.String);
                    DataAccess.AddParam(cmd, "opensAt", location.OpensAt, DbType.DateTime);
                    DataAccess.AddParam(cmd, "closesAt", location.ClosesAt, DbType.DateTime);
                    DataAccess.AddParam(cmd, "description", location.Description ?? string.Empty, DbType.String);
                    DataAccess.AddParam(cmd, "activeFlag", location.ActiveFlag ? "Y" : "N", DbType.String);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Location location)
        {
            var locationId = location.Id;
            if (locationId <= 0)
                throw new InvalidOperationException("Location Id must be a numeric ID.");

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
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
                DataAccess.AddParam(cmd, "name", location.Name ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "address", location.Address ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "phone", location.Phone ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "opensAt", location.OpensAt, DbType.DateTime);
                DataAccess.AddParam(cmd, "closesAt", location.ClosesAt, DbType.DateTime);
                DataAccess.AddParam(cmd, "description", location.Description ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "activeFlag", location.ActiveFlag ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "locationId", locationId, DbType.Int64);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Location> ListAll()
        {
            var list = new List<Location>();
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
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
                Id = locationId,
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

