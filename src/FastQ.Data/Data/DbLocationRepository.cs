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
                @"SELECT ENTITY_ID, ENTITY_NAME, ADDRESS, PHONE, OPENS_AT, CLOSES_AT, DESCRIPTION, ACTIVEFLAG
                  FROM VALIDENTITIES
                  WHERE ENTITY_ID = :locationId"))
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
                    using (var cmd = DataAccess.CreateCommand(conn, "SELECT NVL(MAX(ENTITY_ID),0) + 1 FROM fqowner.VALIDENTITIES"))
                    {
                        locationId = Convert.ToInt64(cmd.ExecuteScalar());
                    }
                    location.Id = locationId;
                }

                using (var cmd = DataAccess.CreateCommand(conn,
                    @"INSERT INTO VALIDENTITIES
                        (ENTITY_ID, ENTITY_NAME, ADDRESS, PHONE, OPENS_AT, CLOSES_AT, DESCRIPTION, ACTIVEFLAG)
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
                @"UPDATE VALIDENTITIES
                  SET ENTITY_NAME = :name,
                      ADDRESS = :address,
                      PHONE = :phone,
                      OPENS_AT = :opensAt,
                      CLOSES_AT = :closesAt,
                      DESCRIPTION = :description,
                      ACTIVEFLAG = :activeFlag
                  WHERE ENTITY_ID = :locationId"))
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
                @"SELECT ENTITY_ID, ENTITY_NAME, ADDRESS, PHONE, OPENS_AT, CLOSES_AT, DESCRIPTION, ACTIVEFLAG
                  FROM VALIDENTITIES"))
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
            var locationId = ReadInt64(record, "ENTITY_ID", "LOCATION_ID");
            var opensAt = record["OPENS_AT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["OPENS_AT"]);
            var closesAt = record["CLOSES_AT"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(record["CLOSES_AT"]);
            var activeFlag = (record["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y";
            return new Location
            {
                Id = locationId,
                Name = ReadString(record, "ENTITY_NAME", "LOCNAME"),
                Address = record["ADDRESS"]?.ToString() ?? string.Empty,
                Phone = record["PHONE"]?.ToString() ?? string.Empty,
                OpensAt = opensAt,
                ClosesAt = closesAt,
                Description = record["DESCRIPTION"]?.ToString() ?? string.Empty,
                ActiveFlag = activeFlag,
                TimeZoneId = "UTC"
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
