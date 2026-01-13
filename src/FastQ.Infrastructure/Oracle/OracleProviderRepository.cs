using System;
using System.Collections.Generic;
using System.Data;
using FastQ.Domain.Entities;
using FastQ.Domain.Repositories;
using FastQ.Infrastructure.Common;

namespace FastQ.Infrastructure.Oracle
{
    public sealed class OracleProviderRepository : IProviderRepository
    {
        private readonly string _connectionString;

        public OracleProviderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Provider Get(Guid id)
        {
            if (!IdMapper.TryToLong(id, out var providerId)) return null;
            var providerKey = providerId.ToString();

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT USER_ID, FNAME, LNAME
                  FROM FQUSERS
                  WHERE USER_ID = :userId"))
            {
                OracleDb.AddParam(cmd, "userId", providerKey, DbType.String);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapProvider(reader, Guid.Empty) : null;
                }
            }
        }

        public void Add(Provider provider)
        {
            if (!IdMapper.TryToLong(provider.Id, out var providerId))
                throw new InvalidOperationException("Provider Id is not mapped to a numeric ID.");

            SplitName(provider.Name, out var first, out var last);
            var email = $"{providerId}@placeholder.local";

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"INSERT INTO FQUSERS
                    (USER_ID, FNAME, LNAME, EMAIL, ACTIVEFLAG, ADMINFLAG, STAMPDATE, STAMPUSER)
                  VALUES
                    (:userId, :fname, :lname, :email, 'Y', 'N', SYSDATE, :stampUser)"))
            {
                OracleDb.AddParam(cmd, "userId", providerId.ToString(), DbType.String);
                OracleDb.AddParam(cmd, "fname", first, DbType.String);
                OracleDb.AddParam(cmd, "lname", last, DbType.String);
                OracleDb.AddParam(cmd, "email", email, DbType.String);
                OracleDb.AddParam(cmd, "stampUser", "fastq", DbType.String);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Provider> ListByLocation(Guid locationId)
        {
            if (!IdMapper.TryToLong(locationId, out var locId)) return new List<Provider>();

            var list = new List<Provider>();
            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT DISTINCT u.USER_ID, u.FNAME, u.LNAME
                  FROM FQUSERS u
                  JOIN USER_PERMISSIONS p ON p.USER_ID = u.USER_ID
                  JOIN VALIDQUEUES q ON q.QUEUE_ID = p.QUEUE_ID
                  WHERE q.LOCATION_ID = :locationId"))
            {
                OracleDb.AddParam(cmd, "locationId", locId, DbType.Int64);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var provider = MapProvider(reader, IdMapper.FromLong(locId));
                        if (provider != null) list.Add(provider);
                    }
                }
            }

            return list;
        }

        public IList<Provider> ListAll()
        {
            var list = new List<Provider>();
            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT USER_ID, FNAME, LNAME
                  FROM FQUSERS
                  WHERE NVL(ACTIVEFLAG, 'Y') = 'Y'"))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var provider = MapProvider(reader, Guid.Empty);
                    if (provider != null) list.Add(provider);
                }
            }

            return list;
        }

        private static Provider MapProvider(IDataRecord record, Guid locationId)
        {
            var userIdText = record["USER_ID"]?.ToString() ?? string.Empty;
            if (!long.TryParse(userIdText, out var userId))
            {
                return null;
            }

            var name = $"{record["FNAME"]} {record["LNAME"]}".Trim();
            return new Provider
            {
                Id = IdMapper.FromLong(userId),
                LocationId = locationId == Guid.Empty ? Guid.Empty : locationId,
                Name = name
            };
        }

        private static void SplitName(string name, out string first, out string last)
        {
            var trimmed = (name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                first = "Unknown";
                last = "Provider";
                return;
            }

            var parts = trimmed.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            first = parts.Length > 0 ? parts[0] : "Unknown";
            last = parts.Length > 1 ? parts[1] : "Provider";
        }
    }
}
