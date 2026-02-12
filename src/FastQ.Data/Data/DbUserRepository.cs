using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
//using Oracle.ManagedDataAccess.Client;


namespace FastQ.Data.Db
{
    public sealed class DbUserRepository : IUserRepository
    {      
        public DbUserRepository()
        {    
        }

        public User Get(string uid, string stampuser)
        {
            //return new User { FirstName = "DB-First", LastName = "DB-Last", UserId = "uid" };
            using (var conn = DataAccess.Open())
            {
                //var locationByQueue = LoadQueueLocations(conn);
                using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS_GET.GET_USER"))
                {
                    DataAccess.AddParam(cmd, "p_userid", uid, DbType.String);
                    DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                    DataAccess.AddParam(cmd, "p_message", string.Empty, DbType.String).Direction = ParameterDirection.Output;
                    cmd.Parameters["p_message"].Size = 2000;
                    DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (cmd.Parameters["p_message"].Value.ToString() != string.Empty)
                        {
                            throw new Exception(cmd.Parameters["p_message"].Value.ToString());
                        }
                        return reader.Read() ? MapUser(reader) : null;
                    }
                }
            }
        }

        public void Add(User ouser)
        {
            var first = "";
            var last = "";
            if (!string.IsNullOrWhiteSpace(ouser.FirstName)) first = ouser.FirstName;
            if (!string.IsNullOrWhiteSpace(ouser.LastName)) last = ouser.LastName;
            var email = string.IsNullOrWhiteSpace(ouser.Email) ? $"{ouser}@placeholder.local" : ouser.Email;
            var stampUser = string.IsNullOrWhiteSpace(ouser.StampUser) ? "fastq" : ouser.StampUser;
            var activeFlag = ouser.ActiveFlag ? "Y" : "N";
            var adminFlag = ouser.AdminFlag ? "Y" : "N";

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"INSERT INTO FQUSERS
                    (USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE, ACTIVEFLAG, ADMINFLAG, PASSWORD, TITLE, STAMPDATE, STAMPUSER)
                  VALUES
                    (:userId, :fname, :lname, :email, :phone, :language, :activeFlag, :adminFlag, :password, :title, SYSDATE, :stampUser)"))
            {
                DataAccess.AddParam(cmd, "userId", ouser.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "fname", first, DbType.String);
                DataAccess.AddParam(cmd, "lname", last, DbType.String);
                DataAccess.AddParam(cmd, "email", email, DbType.String);
                DataAccess.AddParam(cmd, "phone", ouser.Phone ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "language", ouser.Language ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "activeFlag", activeFlag, DbType.String);
                DataAccess.AddParam(cmd, "adminFlag", adminFlag, DbType.String);
                //DataAccess.AddParam(cmd, "password", ouser.Password ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "password", string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "title", ouser.Title ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "stampUser", stampUser, DbType.String);
                cmd.ExecuteNonQuery();
            }        
        }

        public void Update(User ouser)
        {
            //if (!IdMapper.TryToLong(customer.Id, out var customerId))
            //    throw new InvalidOperationException("Customer Id is not mapped to a numeric ID.");

            //SplitName(customer.Name, out var first, out var last);
            //if (!string.IsNullOrWhiteSpace(customer.FirstName)) first = customer.FirstName;
            //if (!string.IsNullOrWhiteSpace(customer.LastName)) last = customer.LastName;
            //var email = BuildPlaceholderEmail(customer, first, last);
            //var stampUser = string.IsNullOrWhiteSpace(customer.StampUser) ? "fastq" : customer.StampUser;
            //var activeFlag = customer.ActiveFlag ? "Y" : "N";

            //using (var conn = OracleDb.Open(_connectionString))
            //using (var cmd = OracleDb.CreateCommand(conn,
            //    @"UPDATE CUSTOMERS
            //      SET FNAME = :fname,
            //          LNAME = :lname,
            //          EMAIL = :email,
            //          PHONE = :phone,
            //          SMS_OPTIN = :smsOptIn,
            //          ACTIVEFLAG = :activeFlag,
            //          STAMPUSER = :stampUser,
            //          STAMPDATE = SYSDATE
            //      WHERE CUSTOMER_ID = :customerId"))
            //{
            //    OracleDb.AddParam(cmd, "fname", Encoding.UTF8.GetBytes(first), DbType.Binary);
            //    OracleDb.AddParam(cmd, "lname", Encoding.UTF8.GetBytes(last), DbType.Binary);
            //    OracleDb.AddParam(cmd, "email", Encoding.UTF8.GetBytes(email), DbType.Binary);
            //    OracleDb.AddParam(cmd, "phone", Encoding.UTF8.GetBytes(customer.Phone ?? string.Empty), DbType.Binary);
            //    OracleDb.AddParam(cmd, "smsOptIn", customer.SmsOptIn ? "Y" : "N", DbType.String);
            //    OracleDb.AddParam(cmd, "activeFlag", activeFlag, DbType.String);
            //    OracleDb.AddParam(cmd, "stampUser", stampUser, DbType.String);
            //    OracleDb.AddParam(cmd, "customerId", customerId, DbType.Int64);
            //    cmd.ExecuteNonQuery();
            //}
        }

        public IList<User> ListAll(Int32 entityid, string stampuser)
        {
            var list = new List<User>();
            using (var conn = DataAccess.Open())
            {
                //var locationByQueue = LoadQueueLocations(conn);
                using (var cmd = DataAccess.CreateStoredProc(conn, "FQ_PROCS_GET.GET_USERS"))
                {
                    DataAccess.AddParam(cmd, "p_entityid", entityid, DbType.Int32); 
                    DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                    DataAccess.AddParam(cmd, "p_message", string.Empty, DbType.String).Direction = ParameterDirection.Output;
                    cmd.Parameters["p_message"].Size = 2000;
                    DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (cmd.Parameters["p_message"].Value.ToString() != string.Empty)
                        {
                            throw new Exception(cmd.Parameters["p_message"].Value.ToString());
                        }
                        while (reader.Read())
                        {
                            list.Add(MapUser(reader));
                        }
                    }
                }
            }

            return list;
        }

        private static User MapUser(IDataRecord record)
        {
            var userIdText = record["USER_ID"]?.ToString() ?? string.Empty;
            if (userIdText == string.Empty)
            {
                return null;
            }

            var first = record["FNAME"]?.ToString() ?? string.Empty;
            var last = record["LNAME"]?.ToString() ?? string.Empty;
            var stampDate = record["STAMPDATE"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(record["STAMPDATE"]);
            var activeFlag = (record["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y";
            var adminFlag = (record["ADMINFLAG"]?.ToString() ?? "N") == "Y";
            return new User
            {
                UserId = record["USER_ID"]?.ToString() ?? string.Empty,
                //LocationId = locationId == Guid.Empty ? Guid.Empty : locationId,
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

        private static string ReadRawString(IDataRecord record, string field)
        {
            var ordinal = record.GetOrdinal(field);
            if (record.IsDBNull(ordinal)) return string.Empty;
            var bytes = (byte[])record.GetValue(ordinal);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }


    }
}

