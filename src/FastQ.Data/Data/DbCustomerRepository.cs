using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Data.Db
{
    public sealed class DbCustomerRepository : ICustomerRepository
    {
        public DbCustomerRepository()
        {
        }

        public Customer Get(long id)
        {
            if (id <= 0) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"SELECT CUSTOMER_ID, FNAME, LNAME, EMAIL, PHONE, SMS_OPTIN, ACTIVEFLAG, STAMPDATE, STAMPUSER
                  FROM CUSTOMERS
                  WHERE CUSTOMER_ID = :customerId"))
            {
                DataAccess.AddParam(cmd, "customerId", id, DbType.Int64);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapCustomer(reader) : null;
                }
            }
        }

        public Customer GetByPhone(string phone)
        {
            phone = phone?.Trim();
            if (string.IsNullOrEmpty(phone)) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"SELECT CUSTOMER_ID, FNAME, LNAME, EMAIL, PHONE, SMS_OPTIN, ACTIVEFLAG, STAMPDATE, STAMPUSER
                  FROM CUSTOMERS
                  WHERE PHONE = :phone"))
            {
                DataAccess.AddParam(cmd, "phone", Encoding.UTF8.GetBytes(phone), DbType.Binary);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapCustomer(reader) : null;
                }
            }
        }

        public void Add(Customer customer)
        {
            using (var conn = DataAccess.Open())
            {
                var newId = DataAccess.NextVal(conn, "CUSTOMERSEQ");
                customer.Id = newId;

                SplitName(customer.Name, out var first, out var last);
                if (!string.IsNullOrWhiteSpace(customer.FirstName)) first = customer.FirstName;
                if (!string.IsNullOrWhiteSpace(customer.LastName)) last = customer.LastName;
                var email = BuildPlaceholderEmail(customer, first, last);
                var stampUser = string.IsNullOrWhiteSpace(customer.StampUser) ? "fastq" : customer.StampUser;
                var activeFlag = customer.ActiveFlag ? "Y" : "N";

                using (var cmd = DataAccess.CreateCommand(conn,
                    @"INSERT INTO CUSTOMERS
                        (CUSTOMER_ID, FNAME, LNAME, EMAIL, PHONE, SMS_OPTIN, ACTIVEFLAG, STAMPUSER, STAMPDATE)
                      VALUES
                        (:customerId, :fname, :lname, :email, :phone, :smsOptIn, :activeFlag, :stampUser, SYSDATE)"))
                {
                    DataAccess.AddParam(cmd, "customerId", newId, DbType.Int64);
                    DataAccess.AddParam(cmd, "fname", Encoding.UTF8.GetBytes(first), DbType.Binary);
                    DataAccess.AddParam(cmd, "lname", Encoding.UTF8.GetBytes(last), DbType.Binary);
                    DataAccess.AddParam(cmd, "email", Encoding.UTF8.GetBytes(email), DbType.Binary);
                    DataAccess.AddParam(cmd, "phone", Encoding.UTF8.GetBytes(customer.Phone ?? string.Empty), DbType.Binary);
                    DataAccess.AddParam(cmd, "smsOptIn", customer.SmsOptIn ? "Y" : "N", DbType.String);
                    DataAccess.AddParam(cmd, "activeFlag", activeFlag, DbType.String);
                    DataAccess.AddParam(cmd, "stampUser", stampUser, DbType.String);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Customer customer)
        {
            var customerId = customer.Id;
            if (customerId <= 0)
                throw new InvalidOperationException("Customer Id must be a numeric ID.");

            SplitName(customer.Name, out var first, out var last);
            if (!string.IsNullOrWhiteSpace(customer.FirstName)) first = customer.FirstName;
            if (!string.IsNullOrWhiteSpace(customer.LastName)) last = customer.LastName;
            var email = BuildPlaceholderEmail(customer, first, last);
            var stampUser = string.IsNullOrWhiteSpace(customer.StampUser) ? "fastq" : customer.StampUser;
            var activeFlag = customer.ActiveFlag ? "Y" : "N";

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"UPDATE CUSTOMERS
                  SET FNAME = :fname,
                      LNAME = :lname,
                      EMAIL = :email,
                      PHONE = :phone,
                      SMS_OPTIN = :smsOptIn,
                      ACTIVEFLAG = :activeFlag,
                      STAMPUSER = :stampUser,
                      STAMPDATE = SYSDATE
                  WHERE CUSTOMER_ID = :customerId"))
            {
                DataAccess.AddParam(cmd, "fname", Encoding.UTF8.GetBytes(first), DbType.Binary);
                DataAccess.AddParam(cmd, "lname", Encoding.UTF8.GetBytes(last), DbType.Binary);
                DataAccess.AddParam(cmd, "email", Encoding.UTF8.GetBytes(email), DbType.Binary);
                DataAccess.AddParam(cmd, "phone", Encoding.UTF8.GetBytes(customer.Phone ?? string.Empty), DbType.Binary);
                DataAccess.AddParam(cmd, "smsOptIn", customer.SmsOptIn ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "activeFlag", activeFlag, DbType.String);
                DataAccess.AddParam(cmd, "stampUser", stampUser, DbType.String);
                DataAccess.AddParam(cmd, "customerId", customerId, DbType.Int64);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Customer> ListAll()
        {
            var list = new List<Customer>();
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn,
                @"SELECT CUSTOMER_ID, FNAME, LNAME, EMAIL, PHONE, SMS_OPTIN, ACTIVEFLAG, STAMPDATE, STAMPUSER
                  FROM CUSTOMERS"))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(MapCustomer(reader));
                }
            }

            return list;
        }

        private static Customer MapCustomer(IDataRecord record)
        {
            var id = Convert.ToInt64(record["CUSTOMER_ID"]);
            var first = ReadRawString(record, "FNAME");
            var last = ReadRawString(record, "LNAME");
            var email = ReadRawString(record, "EMAIL");
            var phone = ReadRawString(record, "PHONE");
            var smsOptIn = (record["SMS_OPTIN"]?.ToString() ?? string.Empty) == "Y";
            var activeFlag = (record["ACTIVEFLAG"]?.ToString() ?? "Y") == "Y";
            var stampDate = record["STAMPDATE"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(record["STAMPDATE"]);
            var stampUser = record["STAMPUSER"]?.ToString() ?? string.Empty;

            return new Customer
            {
                Id = id,
                FirstName = first,
                LastName = last,
                Email = email,
                Phone = phone,
                SmsOptIn = smsOptIn,
                ActiveFlag = activeFlag,
                StampUser = stampUser,
                CreatedUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Utc),
                UpdatedUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Utc),
                StampDateUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Utc)
            };
        }

        private static string ReadRawString(IDataRecord record, string field)
        {
            var ordinal = record.GetOrdinal(field);
            if (record.IsDBNull(ordinal)) return string.Empty;
            var bytes = (byte[])record.GetValue(ordinal);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void SplitName(string name, out string first, out string last)
        {
            var trimmed = (name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                first = "Unknown";
                last = "Customer";
                return;
            }

            var parts = trimmed.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            first = parts.Length > 0 ? parts[0] : "Unknown";
            last = parts.Length > 1 ? parts[1] : "Customer";
        }

        private static string BuildPlaceholderEmail(Customer customer, string first, string last)
        {
            if (!string.IsNullOrWhiteSpace(customer.Phone))
            {
                return $"{customer.Phone}@placeholder.local";
            }

            return $"{first}.{last}@placeholder.local".Replace(" ", string.Empty).ToLowerInvariant();
        }
    }
}

