using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Data.Common;

namespace FastQ.Data.Oracle
{
    public sealed class OracleCustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public OracleCustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Customer Get(Guid id)
        {
            if (!IdMapper.TryToLong(id, out var customerId)) return null;

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT CUSTOMER_ID, FNAME, LNAME, EMAIL, PHONE, SMS_OPTIN, STAMPDATE
                  FROM CUSTOMERS
                  WHERE CUSTOMER_ID = :customerId"))
            {
                OracleDb.AddParam(cmd, "customerId", customerId, DbType.Int64);
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

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT CUSTOMER_ID, FNAME, LNAME, EMAIL, PHONE, SMS_OPTIN, STAMPDATE
                  FROM CUSTOMERS
                  WHERE PHONE = :phone"))
            {
                OracleDb.AddParam(cmd, "phone", Encoding.UTF8.GetBytes(phone), DbType.Binary);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapCustomer(reader) : null;
                }
            }
        }

        public void Add(Customer customer)
        {
            using (var conn = OracleDb.Open(_connectionString))
            {
                var newId = OracleDb.NextVal(conn, "CUSTOMERSEQ");
                customer.Id = IdMapper.FromLong(newId);

                SplitName(customer.Name, out var first, out var last);
                var email = BuildPlaceholderEmail(customer, first, last);

                using (var cmd = OracleDb.CreateCommand(conn,
                    @"INSERT INTO CUSTOMERS
                        (CUSTOMER_ID, FNAME, LNAME, EMAIL, PHONE, SMS_OPTIN, ACTIVEFLAG, STAMPUSER, STAMPDATE)
                      VALUES
                        (:customerId, :fname, :lname, :email, :phone, :smsOptIn, 'Y', :stampUser, SYSDATE)"))
                {
                    OracleDb.AddParam(cmd, "customerId", newId, DbType.Int64);
                    OracleDb.AddParam(cmd, "fname", Encoding.UTF8.GetBytes(first), DbType.Binary);
                    OracleDb.AddParam(cmd, "lname", Encoding.UTF8.GetBytes(last), DbType.Binary);
                    OracleDb.AddParam(cmd, "email", Encoding.UTF8.GetBytes(email), DbType.Binary);
                    OracleDb.AddParam(cmd, "phone", Encoding.UTF8.GetBytes(customer.Phone ?? string.Empty), DbType.Binary);
                    OracleDb.AddParam(cmd, "smsOptIn", customer.SmsOptIn ? "Y" : "N", DbType.String);
                    OracleDb.AddParam(cmd, "stampUser", "fastq", DbType.String);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Customer customer)
        {
            if (!IdMapper.TryToLong(customer.Id, out var customerId))
                throw new InvalidOperationException("Customer Id is not mapped to a numeric ID.");

            SplitName(customer.Name, out var first, out var last);
            var email = BuildPlaceholderEmail(customer, first, last);

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"UPDATE CUSTOMERS
                  SET FNAME = :fname,
                      LNAME = :lname,
                      EMAIL = :email,
                      PHONE = :phone,
                      SMS_OPTIN = :smsOptIn,
                      STAMPUSER = :stampUser,
                      STAMPDATE = SYSDATE
                  WHERE CUSTOMER_ID = :customerId"))
            {
                OracleDb.AddParam(cmd, "fname", Encoding.UTF8.GetBytes(first), DbType.Binary);
                OracleDb.AddParam(cmd, "lname", Encoding.UTF8.GetBytes(last), DbType.Binary);
                OracleDb.AddParam(cmd, "email", Encoding.UTF8.GetBytes(email), DbType.Binary);
                OracleDb.AddParam(cmd, "phone", Encoding.UTF8.GetBytes(customer.Phone ?? string.Empty), DbType.Binary);
                OracleDb.AddParam(cmd, "smsOptIn", customer.SmsOptIn ? "Y" : "N", DbType.String);
                OracleDb.AddParam(cmd, "stampUser", "fastq", DbType.String);
                OracleDb.AddParam(cmd, "customerId", customerId, DbType.Int64);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Customer> ListAll()
        {
            var list = new List<Customer>();
            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT CUSTOMER_ID, FNAME, LNAME, EMAIL, PHONE, SMS_OPTIN, STAMPDATE
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
            var phone = ReadRawString(record, "PHONE");
            var smsOptIn = (record["SMS_OPTIN"]?.ToString() ?? string.Empty) == "Y";
            var stampDate = record["STAMPDATE"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(record["STAMPDATE"]);

            return new Customer
            {
                Id = IdMapper.FromLong(id),
                Name = $"{first} {last}".Trim(),
                Phone = phone,
                SmsOptIn = smsOptIn,
                CreatedUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Utc),
                UpdatedUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Utc)
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

