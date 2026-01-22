using System;
using System.Data;
using System.Data.Common;

namespace FastQ.Data.Oracle
{
    internal static class OracleDb
    {
        public const string ProviderName = "Oracle.ManagedDataAccess.Client";

        public static DbConnection Open(string connectionString)
        {
            var factory = DbProviderFactories.GetFactory(ProviderName);
            var conn = factory.CreateConnection();
            if (conn == null) throw new InvalidOperationException($"Provider factory '{ProviderName}' not available.");
            conn.ConnectionString = connectionString;
            conn.Open();
            return conn;
        }

        public static DbCommand CreateCommand(DbConnection conn, string sql)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            return cmd;
        }

        public static DbCommand CreateStoredProc(DbConnection conn, string procedureName)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = procedureName;
            cmd.CommandType = CommandType.StoredProcedure;
            return cmd;
        }

        public static DbParameter AddParam(DbCommand cmd, string name, object value, DbType? type = null)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            if (type.HasValue) p.DbType = type.Value;
            cmd.Parameters.Add(p);
            return p;
        }

        public static DbParameter AddOutRefCursor(DbCommand cmd, string name)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Direction = ParameterDirection.Output;
            p.DbType = DbType.Object;
            TrySetOracleRefCursor(p);
            cmd.Parameters.Add(p);
            return p;
        }

        public static long NextVal(DbConnection conn, string sequenceName)
        {
            using (var cmd = CreateCommand(conn, $"SELECT {sequenceName}.NEXTVAL FROM DUAL"))
            {
                var val = cmd.ExecuteScalar();
                return Convert.ToInt64(val);
            }
        }

        private static void TrySetOracleRefCursor(DbParameter parameter)
        {
            var prop = parameter.GetType().GetProperty("OracleDbType");
            if (prop == null)
            {
                return;
            }

            var enumType = prop.PropertyType;
            var value = Enum.Parse(enumType, "RefCursor");
            prop.SetValue(parameter, value, null);
        }
    }
}

