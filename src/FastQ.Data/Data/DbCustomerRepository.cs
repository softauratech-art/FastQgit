using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Helpers;

namespace FastQ.Data.Db
{
    public sealed class DbCustomerRepository : ICustomerRepository
    {
        public Customer Get(long id)
        {
            //@"SELECT CUSTOMER_ID,
            //             FQ_CRYPTO_PKG.DECRYPT(FNAME) AS FNAME,
            //             FQ_CRYPTO_PKG.DECRYPT(LNAME) AS LNAME,
            //             FQ_CRYPTO_PKG.DECRYPT(EMAIL) AS EMAIL,
            //             FQ_CRYPTO_PKG.DECRYPT(PHONE) AS PHONE,
            //             SMS_OPTIN, ACTIVEFLAG, STAMPDATE, STAMPUSER
            //      FROM CUSTOMERS
            //      WHERE CUSTOMER_ID = :customerId"
            
            if (id <= 0) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_CUSTOMERS"))
            {
                DataAccess.AddParam(cmd, "p_fldname", "CUSTOMER_ID", DbType.String);
                DataAccess.AddParam(cmd, "p_fldvalue", id.ToString(), DbType.String);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapCustomer(reader) : null;
                }
            }
        }

        public Customer GetByPhone(string phone)
        {
            //@"SELECT CUSTOMER_ID,
            //             FQ_CRYPTO_PKG.DECRYPT(FNAME) AS FNAME,
            //             FQ_CRYPTO_PKG.DECRYPT(LNAME) AS LNAME,
            //             FQ_CRYPTO_PKG.DECRYPT(EMAIL) AS EMAIL,
            //             FQ_CRYPTO_PKG.DECRYPT(PHONE) AS PHONE,
            //             SMS_OPTIN, ACTIVEFLAG, STAMPDATE, STAMPUSER
            //      FROM CUSTOMERS
            //      WHERE lower(FQ_CRYPTO_PKG.DECRYPT(PHONE)) = lower(:phone)"

            phone = phone?.Trim();
            if (string.IsNullOrEmpty(phone)) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_CUSTOMERS"))
            {
                DataAccess.AddParam(cmd, "p_fldname", "PHONE", DbType.String);
                DataAccess.AddParam(cmd, "p_fldvalue", phone, DbType.String);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapCustomer(reader) : null;
                }
            }
        }

        public Customer GetByEmail(string email)
        {
            //@"SELECT CUSTOMER_ID,
            //             FQ_CRYPTO_PKG.DECRYPT(FNAME) AS FNAME,
            //             FQ_CRYPTO_PKG.DECRYPT(LNAME) AS LNAME,
            //             FQ_CRYPTO_PKG.DECRYPT(EMAIL) AS EMAIL,
            //             FQ_CRYPTO_PKG.DECRYPT(PHONE) AS PHONE,
            //             SMS_OPTIN, ACTIVEFLAG, STAMPDATE, STAMPUSER
            //      FROM CUSTOMERS
            //      WHERE lower(FQ_CRYPTO_PKG.DECRYPT(EMAIL)) = lower(:email)"

            email = email?.Trim();
            if (string.IsNullOrEmpty(email)) return null;

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_CUSTOMERS"))
            {
                DataAccess.AddParam(cmd, "p_fldname" , "EMAIL", DbType.String);
                DataAccess.AddParam(cmd, "p_fldvalue", email, DbType.String);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
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
                SplitName(customer.Name, out var first, out var last);
                if (!string.IsNullOrWhiteSpace(customer.FirstName)) first = customer.FirstName;
                if (!string.IsNullOrWhiteSpace(customer.LastName)) last = customer.LastName;
                var email = BuildPlaceholderEmail(customer, first, last);
                var stampUser = string.IsNullOrWhiteSpace(customer.StampUser) ? "fastq" : customer.StampUser;
                var activeFlag = customer.ActiveFlag ? "Y" : "N";

                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS.INSERT_CUSTOMER"))
                {
                    DataAccess.AddParam(cmd, "p_fname", first, DbType.String);
                    DataAccess.AddParam(cmd, "p_lname", last, DbType.String);
                    DataAccess.AddParam(cmd, "p_email", email, DbType.String);
                    DataAccess.AddParam(cmd, "p_phone", customer.Phone ?? string.Empty, DbType.String);
                    DataAccess.AddParam(cmd, "p_sms_optin", customer.SmsOptIn ? "Y" : "N", DbType.String);
                    DataAccess.AddParam(cmd, "p_activeflag", activeFlag, DbType.String);
                    DataAccess.AddParam(cmd, "p_stampuser", stampUser, DbType.String);

                    var outId = DataAccess.AddParam(cmd, "p_customer_id", null, DbType.Int64);
                    outId.Direction = ParameterDirection.Output;
                    var outMsg = DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String);
                    outMsg.Direction = ParameterDirection.Output;
                    outMsg.Size = 4000;

                    cmd.ExecuteNonQuery();

                    var error = outMsg.Value == DBNull.Value ? string.Empty : outMsg.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(error))
                        throw new InvalidOperationException(error);

                    if (outId.Value != DBNull.Value && outId.Value != null)
                        customer.Id = Convert.ToInt64(outId.Value);
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
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS.UPDATE_CUSTOMER"))
            {
                DataAccess.AddParam(cmd, "p_customer_id", customerId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_fname", first, DbType.String);
                DataAccess.AddParam(cmd, "p_lname", last, DbType.String);
                DataAccess.AddParam(cmd, "p_email", email, DbType.String);
                DataAccess.AddParam(cmd, "p_phone", customer.Phone ?? string.Empty, DbType.String);
                DataAccess.AddParam(cmd, "p_sms_optin", customer.SmsOptIn ? "Y" : "N", DbType.String);
                DataAccess.AddParam(cmd, "p_activeflag", activeFlag, DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampUser, DbType.String);

                var outMsg = DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String);
                outMsg.Direction = ParameterDirection.Output;
                outMsg.Size = 4000;

                cmd.ExecuteNonQuery();

                var error = outMsg.Value == DBNull.Value ? string.Empty : outMsg.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(error))
                    throw new InvalidOperationException(error);
            }
        }

        public IList<Customer> ListAll()
        {
            //@"SELECT CUSTOMER_ID,
            //             FQ_CRYPTO_PKG.DECRYPT(FNAME) AS FNAME,
            //             FQ_CRYPTO_PKG.DECRYPT(LNAME) AS LNAME,
            //             FQ_CRYPTO_PKG.DECRYPT(EMAIL) AS EMAIL,
            //             FQ_CRYPTO_PKG.DECRYPT(PHONE) AS PHONE,
            //             SMS_OPTIN, ACTIVEFLAG, STAMPDATE, STAMPUSER
            //      FROM CUSTOMERS"
            
            var list = new List<Customer>();
            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_CUSTOMERS"))
            {
                DataAccess.AddParam(cmd, "p_fldname", "ALL", DbType.String);
                DataAccess.AddParam(cmd, "p_fldvalue", "", DbType.String);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapCustomer(reader));
                    }
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
            var value = record.GetValue(ordinal);
            if (value is byte[] bytes)
            {
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            return value?.ToString() ?? string.Empty;
        }

        private static void SplitName(string name, out string first, out string last)
        {
            var trimmed = (name ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                first = string.Empty;
                last = string.Empty;
                return;
            }

            var parts = trimmed.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            first = parts.Length > 0 ? parts[0] : string.Empty;
            last = parts.Length > 1 ? parts[1] : string.Empty;
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
