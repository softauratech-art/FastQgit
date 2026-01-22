using System;
using System.Collections.Generic;
using System.Data;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Data.Common;

namespace FastQ.Data.Oracle
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
                @"SELECT USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE, ACTIVEFLAG, ADMINFLAG, TITLE, STAMPDATE, STAMPUSER
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
            if (!string.IsNullOrWhiteSpace(provider.FirstName)) first = provider.FirstName;
            if (!string.IsNullOrWhiteSpace(provider.LastName)) last = provider.LastName;
            var email = string.IsNullOrWhiteSpace(provider.Email) ? $"{providerId}@placeholder.local" : provider.Email;
            var stampUser = string.IsNullOrWhiteSpace(provider.StampUser) ? "fastq" : provider.StampUser;
            var activeFlag = provider.ActiveFlag ? "Y" : "N";
            var adminFlag = provider.AdminFlag ? "Y" : "N";

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"INSERT INTO FQUSERS
                    (USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE, ACTIVEFLAG, ADMINFLAG, PASSWORD, TITLE, STAMPDATE, STAMPUSER)
                  VALUES
                    (:userId, :fname, :lname, :email, :phone, :language, :activeFlag, :adminFlag, :password, :title, SYSDATE, :stampUser)"))
            {
                OracleDb.AddParam(cmd, "userId", providerId.ToString(), DbType.String);
                OracleDb.AddParam(cmd, "fname", first, DbType.String);
                OracleDb.AddParam(cmd, "lname", last, DbType.String);
                OracleDb.AddParam(cmd, "email", email, DbType.String);
                OracleDb.AddParam(cmd, "phone", provider.Phone ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "language", provider.Language ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "activeFlag", activeFlag, DbType.String);
                OracleDb.AddParam(cmd, "adminFlag", adminFlag, DbType.String);
                OracleDb.AddParam(cmd, "password", provider.Password ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "title", provider.Title ?? string.Empty, DbType.String);
                OracleDb.AddParam(cmd, "stampUser", stampUser, DbType.String);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Provider> ListByLocation(Guid locationId)
        {
            if (!IdMapper.TryToLong(locationId, out var locId)) return new List<Provider>();

            var list = new List<Provider>();
            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT DISTINCT u.USER_ID, u.FNAME, u.LNAME, u.EMAIL, u.PHONE, u.LANGUAGE, u.ACTIVEFLAG, u.ADMINFLAG, u.TITLE, u.STAMPDATE, u.STAMPUSER
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
                @"SELECT USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE, ACTIVEFLAG, ADMINFLAG, TITLE, STAMPDATE, STAMPUSER
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

            var first = record["FNAME"]?.ToString() ?? string.Empty;
            var last = record["LNAME"]?.ToString() ?? string.Empty;
            var stampDate = record["STAMPDATE"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(record["STAMPDATE"]);
            var activeFlag = (record["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y";
            var adminFlag = (record["ADMINFLAG"]?.ToString() ?? "N") == "Y";
            return new Provider
            {
                Id = IdMapper.FromLong(userId),
                LocationId = locationId == Guid.Empty ? Guid.Empty : locationId,
                FirstName = first,
                LastName = last,
                Email = record["EMAIL"]?.ToString() ?? string.Empty,
                Phone = record["PHONE"]?.ToString() ?? string.Empty,
                Language = record["LANGUAGE"]?.ToString() ?? string.Empty,
                ActiveFlag = activeFlag,
                AdminFlag = adminFlag,
                Title = record["TITLE"]?.ToString() ?? string.Empty,
                StampUser = record["STAMPUSER"]?.ToString() ?? string.Empty,
                StampDateUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Utc)
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

