using FastQ.Data.Entities;
using FastQ.Data.Repositories;
//using Microsoft.AspNet.SignalR.Messaging;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Security.Cryptography;


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
            string sp_name =  (stampuser == "AUTHSERVICE") ? "fqowner.AUTHENTICATE_USER" : "fqowner.FQ_PROCS_GET.GET_USER";
            try
            {
                using (var conn = DataAccess.Open())
                {
                    //var locationByQueue = LoadQueueLocations(conn);
                    using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
                    {
                        DataAccess.AddParam(cmd, "p_userid", uid, DbType.String);
                        DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                        DataAccess.AddParam(cmd, "p_message", string.Empty, DbType.String).Direction = ParameterDirection.Output;
                        cmd.Parameters["p_message"].Size = 2000;
                        DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (cmd.Parameters["p_message"]?.Value.ToString() != string.Empty)
                            {
                                return null;
                                //throw new Exception(cmd.Parameters["p_message"].Value.ToString(), ex);
                                //TODO: Log Error
                            }
                            return reader.Read() ? MapUser(reader, stampuser) : null;
                        }
                    }
                }
            }
            catch (OracleException ex)
            {
                return null;
            }
        }

        //public void Add(User ouser)
        //{
        //    var first = "";
        //    var last = "";
        //    if (!string.IsNullOrWhiteSpace(ouser.FirstName)) first = ouser.FirstName;
        //    if (!string.IsNullOrWhiteSpace(ouser.LastName)) last = ouser.LastName;
        //    var email = string.IsNullOrWhiteSpace(ouser.Email) ? $"{ouser}@placeholder.local" : ouser.Email;
        //    var stampUser = string.IsNullOrWhiteSpace(ouser.StampUser) ? "fastq" : ouser.StampUser;
        //    var activeFlag = ouser.ActiveFlag ? "Y" : "N";
        //    var adminFlag = false; // ouser.AdminFlag ? "Y" : "N";

        //    using (var conn = DataAccess.Open())
        //    using (var cmd = DataAccess.CreateCommand(conn,
        //        @"INSERT INTO FQUSERS
        //            (USER_ID, FNAME, LNAME, EMAIL, PHONE, LANGUAGE, ACTIVEFLAG, ADMINFLAG, PASSWORD, TITLE, STAMPDATE, STAMPUSER)
        //          VALUES
        //            (:userId, :fname, :lname, :email, :phone, :language, :activeFlag, :adminFlag, :password, :title, SYSDATE, :stampUser)"))
        //    {
        //        DataAccess.AddParam(cmd, "userId", ouser.ToString(), DbType.String);
        //        DataAccess.AddParam(cmd, "fname", first, DbType.String);
        //        DataAccess.AddParam(cmd, "lname", last, DbType.String);
        //        DataAccess.AddParam(cmd, "email", email, DbType.String);
        //        DataAccess.AddParam(cmd, "phone", ouser.Phone ?? string.Empty, DbType.String);
        //        DataAccess.AddParam(cmd, "language", ouser.Language ?? string.Empty, DbType.String);
        //        DataAccess.AddParam(cmd, "activeFlag", activeFlag, DbType.String);
        //        DataAccess.AddParam(cmd, "adminFlag", adminFlag, DbType.String);
        //        //DataAccess.AddParam(cmd, "password", ouser.Password ?? string.Empty, DbType.String);
        //        DataAccess.AddParam(cmd, "password", string.Empty, DbType.String);
        //        DataAccess.AddParam(cmd, "title", ouser.Title ?? string.Empty, DbType.String);
        //        DataAccess.AddParam(cmd, "stampUser", stampUser, DbType.String);
        //        cmd.ExecuteNonQuery();
        //    }        
        //}

        //public void Update(User ouser)
        //{
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
        //}

        public IList<User> ListAll(Int32 entityid, string stampuser)
        {
            var list = new List<User>();
            using (var conn = DataAccess.Open())
            {
                //var locationByQueue = LoadQueueLocations(conn);
                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_USERS"))
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
                            list.Add(MapUser(reader, stampuser));
                        }
                    }
                }
            }

            return list;
        }

        private static User MapUser(IDataRecord record, string stampuser)
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
                // AdminFlag set in BusinessWEntities collection for each entity
                Title = record["TITLE"]?.ToString() ?? string.Empty,
                StampUser = record["STAMPUSER"]?.ToString() ?? string.Empty,
                //StampDateUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Utc),
                StampDateUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Local),
                Queues = GetUserQueuePermissions(userIdText, stampuser),
                BusinessEntities = GetUserEntities(userIdText, stampuser)
            };
        }

        public static IList<UserQueuePermission> GetUserQueuePermissions(string uid, string stampuser)
        {
            using (var conn = DataAccess.Open())
            {                
                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_USER_QUEUES_ROLES"))
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
                            //TODO: Log Error
                        }
                        //return reader.Read() ? MapUserQPermissions(reader) : null;

                        var list = new List<Entities.UserQueuePermission>();
                        while (reader.Read())
                        {
                            list.Add(new UserQueuePermission { 
                                    UserId = uid, 
                                    QueueId = Convert.ToInt64(reader["QUEUE_ID"]?.ToString()),
                                    HostFlag = (reader["HOST_FLAG"]?.ToString() ?? "Y") == "Y",
                                    ProviderFlag = (reader["provider_flag"]?.ToString() ?? "Y") == "Y",
                                    ReporterFlag = (reader["reporter_Flag"]?.ToString() ?? "Y") == "Y",
                                    QueueAdminFlag = (reader["queueadmin_Flag"]?.ToString() ?? "Y") == "Y",
                                    EntityId  =  ReadInt32(reader, "ENTITY_ID"),
                                    QueueActiveFlag = (reader["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y"
                            });
                        }
                        return list;
                    }
                }
            }
        }

        public static IList<UserEntity> GetUserEntities(string uid, string stampuser)
        {
            using (var conn = DataAccess.Open())
            {
                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.GET_USER_ENTITIES"))
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
                            //TODO: Log Error
                        }

                        var list = new List<Entities.UserEntity>();
                        while (reader.Read())
                        {
                            list.Add(new UserEntity
                            {                                 
                                EntityId = Convert.ToInt32(reader["ENTITYID"]?.ToString()),
                                EntityName = reader["ENTITYNAME"]?.ToString(),
                                ActiveFlag = (reader["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y",
                                ConfigAdminFlag = (reader["ADMINFLAG"]?.ToString() ?? "Y") == "Y"
                            });
                        }
                        return list;
                    }
                }
            }
        }

        public void AddOrUpdateUser(string action, User ouser, long entityid, string hostqueues, string providerqueues, string reporterqueues, string queueadminqueues, string stampuser)
        {
            using var conn = DataAccess.Open();
            string sp_name = "fqowner.FQ_UPSERTUSER";
            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {
                DataAccess.AddParam(cmd, "p_action", action, DbType.String); 
                DataAccess.AddParam(cmd, "p_userid", ouser.UserId, DbType.String);
                DataAccess.AddParam(cmd, "p_entityid", entityid, DbType.Int64);
                DataAccess.AddParam(cmd, "p_firstname", ouser.FirstName, DbType.String);
                DataAccess.AddParam(cmd, "p_lastname", ouser.LastName, DbType.String);
                DataAccess.AddParam(cmd, "p_title", ouser.Title, DbType.String);
                DataAccess.AddParam(cmd, "p_email", ouser.Email, DbType.String);
                DataAccess.AddParam(cmd, "p_phone", ouser.Phone, DbType.String);
                DataAccess.AddParam(cmd, "p_language", ouser.Language, DbType.String);
                DataAccess.AddParam(cmd, "p_activeflag", ouser.ActiveFlag ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_configadminflag", ouser.BusinessEntities[0].ConfigAdminFlag ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_hostqueues", hostqueues, DbType.String);
                DataAccess.AddParam(cmd, "p_providerqueues", providerqueues, DbType.String); 
                DataAccess.AddParam(cmd, "p_reporterqueues", reporterqueues, DbType.String); 
                DataAccess.AddParam(cmd, "p_queueadminqueues", queueadminqueues, DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                string dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (!string.IsNullOrEmpty(dberr)) throw new InvalidOperationException("DB Error: " + dberr);
            }
        }

        public void Delete(string uid, string stampuser)
        {
            using var conn = DataAccess.Open();
            string sp_name = "fqowner.FQ_PROCS_ADMIN.DELETEUSER";
            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {
                DataAccess.AddParam(cmd, "p_userid", uid, DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                var dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (dberr != null) throw new InvalidOperationException(dberr);
            }
        }

        public IList<UserQueuePermission> GetActionQueuePermissions(string uid)
        {
            var list = new List<UserQueuePermission>();
            if (string.IsNullOrWhiteSpace(uid))
            {
                return list;
            }

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_USER_ACTION_QUEUE_ACCESS"))
            {
                DataAccess.AddParam(cmd, "p_userid", uid, DbType.String);
                DataAccess.AddOutRefCursor(cmd, "p_cur");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new UserQueuePermission
                        {
                            UserId = uid,
                            QueueId = Convert.ToInt64(reader["QUEUE_ID"]?.ToString()),
                            QueueName = reader["NAME"]?.ToString() ?? string.Empty,
                            EntityId = ReadInt32(reader, "ENTITY_ID"),
                            HostFlag = (reader["HOST_FLAG"]?.ToString() ?? "N") == "Y",
                            ProviderFlag = (reader["PROVIDER_FLAG"]?.ToString() ?? "N") == "Y",
                            ReporterFlag = (reader["REPORTER_FLAG"]?.ToString() ?? "N") == "Y",
                            QueueAdminFlag = (reader["QUEUEADMIN_FLAG"]?.ToString() ?? "N") == "Y",
                            QueueActiveFlag = (reader["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y"
                        });
                    }
                }
            }

            return list;
        }

        private static int ReadInt32(IDataRecord record, params string[] fieldNames)
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

                    return Convert.ToInt32(record.GetValue(i));
                }
            }

            return 0;
        }
    }
}
