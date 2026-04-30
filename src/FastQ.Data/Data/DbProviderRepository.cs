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

        public IList<Provider> ListByEntity(long entityId)
        {
            if (entityId <= 0) return new List<Provider>();

            var list = new List<Provider>();
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_PROVIDERS_BY_ENTITY"))
            {
                DataAccess.AddParam(cmd, "p_entityid", entityId, DbType.Int64);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var provider = MapProvider(reader, entityId);
                        if (provider != null) list.Add(provider);
                    }
                }
            }

            return list;
        }

        //public IList<Provider> ListAll()
        //{
        //    var list = new List<Provider>();
        //    using (var conn = DataAccess.Open())
        //    using (var cmd = DataAccess.CreateCommand(conn,
        //        @"SELECT USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE, ACTIVEFLAG, ADMINFLAG, TITLE, STAMPDATE, STAMPUSER
        //          FROM fqowner.FQ_USERS
        //          WHERE NVL(ACTIVEFLAG, 'Y') = 'Y'"))
        //    using (var reader = cmd.ExecuteReader())
        //    {
        //        while (reader.Read())
        //        {
        //            var provider = MapProvider(reader, 0);
        //            if (provider != null) list.Add(provider);
        //        }
        //    }
        //    return list;
        //}

        private static Provider MapProvider(IDataRecord record, long entityId)
        {
            var userIdText = record["USER_ID"]?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userIdText)) return null;

            var first = record["FNAME"]?.ToString() ?? string.Empty;
            var last = record["LNAME"]?.ToString() ?? string.Empty;
            var stampDate = record["STAMPDATE"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(record["STAMPDATE"]);
            var activeFlag = (record["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y";
            var adminFlag = (record["ADMINFLAG"]?.ToString() ?? "N") == "Y";
            return new Provider
            {
                Id = userIdText,
                EntityId = entityId,
                FirstName = first,
                LastName = last,
                Email = record["EMAIL"]?.ToString() ?? string.Empty,
                Phone = record["PHONE"]?.ToString() ?? string.Empty,
                Language = record["LANGUAGE"]?.ToString() ?? string.Empty,
                ActiveFlag = activeFlag,
                AdminFlag = adminFlag,
                Title = record["TITLE"]?.ToString() ?? string.Empty,
                StampUser = record["STAMPUSER"]?.ToString() ?? string.Empty,
                StampDate = stampDate
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
