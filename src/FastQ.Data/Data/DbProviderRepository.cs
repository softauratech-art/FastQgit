using System;
using System.Collections.Generic;
using System.Data;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Data.Db
{
    public sealed class DbProviderRepository : IProviderRepository
    {
      
        public DbProviderRepository()
        {
        }

        public Provider Get(string id)
        {
            var providerKey = (id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(providerKey)) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"SELECT USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE, ACTIVEFLAG, ADMINFLAG, TITLE, STAMPDATE, STAMPUSER
                  FROM FQUSERS
                  WHERE USER_ID = :userId"))
            {
                DataAccess.AddParam(cmd, "userId", providerKey, DbType.String);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapProvider(reader, 0) : null;
                }
            }
        }

        public void Add(Provider provider)
        {
            var providerId = (provider.Id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(providerId))
                throw new InvalidOperationException("Provider Id is required.");

            SplitName(provider.Name, out var first, out var last);
            if (!string.IsNullOrWhiteSpace(provider.FirstName)) first = provider.FirstName;
            if (!string.IsNullOrWhiteSpace(provider.LastName)) last = provider.LastName;
            var email = string.IsNullOrWhiteSpace(provider.Email) ? $"{providerId}@placeholder.local" : provider.Email;
            var stampUser = string.IsNullOrWhiteSpace(provider.StampUser) ? "fastq" : provider.StampUser;
            var activeFlag = provider.ActiveFlag ? "Y" : "N";
            var adminFlag = provider.AdminFlag ? "Y" : "N";

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"INSERT INTO FQUSERS
                    (USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE, ACTIVEFLAG, ADMINFLAG, PASSWORD, TITLE, STAMPDATE, STAMPUSER)
                  VALUES
                    (:userId, :fname, :lname, :email, :phone, :language, :activeFlag, :adminFlag, :password, :title, SYSDATE, :stampUser)"))
            {
                DataAccess.AddParam(cmd, "userId", providerId, DbType.String);
                DataAccess.AddParam(cmd, "fname", first, DbType.String);
                DataAccess.AddParam(cmd, "lname", last, DbType.String);
                DataAccess.AddParam(cmd, "email", email, DbType.String);
                DataAccess.AddParam(cmd, "phone", provider.Phone ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "language", provider.Language ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "activeFlag", activeFlag, DbType.String);
                DataAccess.AddParam(cmd, "adminFlag", adminFlag, DbType.String);
                DataAccess.AddParam(cmd, "password", provider.Password ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "title", provider.Title ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "stampUser", stampUser, DbType.String);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Provider> ListByLocation(long locationId)
        {
            if (locationId <= 0) return new List<Provider>();

            var list = new List<Provider>();
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"SELECT DISTINCT u.USER_ID, u.FNAME, u.LNAME, u.EMAIL, u.PHONE, u.LANGUAGE, u.ACTIVEFLAG, u.ADMINFLAG, u.TITLE, u.STAMPDATE, u.STAMPUSER
                  FROM FQUSERS u
                  JOIN USER_PERMISSIONS p ON p.USER_ID = u.USER_ID
                  JOIN VALIDQUEUES q ON q.QUEUE_ID = p.QUEUE_ID
                  WHERE q.LOCATION_ID = :locationId"))
            {
                DataAccess.AddParam(cmd, "locationId", locationId, DbType.Int64);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var provider = MapProvider(reader, locationId);
                        if (provider != null) list.Add(provider);
                    }
                }
            }

            return list;
        }

        public IList<Provider> ListAll()
        {
            var list = new List<Provider>();
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"SELECT USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE, ACTIVEFLAG, ADMINFLAG, TITLE, STAMPDATE, STAMPUSER
                  FROM FQUSERS
                  WHERE NVL(ACTIVEFLAG, 'Y') = 'Y'"))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var provider = MapProvider(reader, 0);
                    if (provider != null) list.Add(provider);
                }
            }

            return list;
        }

        private static Provider MapProvider(IDataRecord record, long locationId)
        {
            var userIdText = record["USER_ID"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userIdText)) return null;

            var first = record["FNAME"]?.ToString() ?? string.Empty;
            var last = record["LNAME"]?.ToString() ?? string.Empty;
            var stampDate = record["STAMPDATE"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(record["STAMPDATE"]);
            var activeFlag = (record["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y";
            var adminFlag = (record["ADMINFLAG"]?.ToString() ?? "N") == "Y";
            return new Provider
            {
                Id = userIdText,
                LocationId = locationId,
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

