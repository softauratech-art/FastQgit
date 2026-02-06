using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

namespace FastQ.Data.Oracle
{
    public sealed class OracleAppointmentRepository : IAppointmentRepository
    {
        private readonly string _connectionString;

        public OracleAppointmentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Appointment Get(long id)
        {
            if (id <= 0) return null;

            using (var conn = OracleDb.Open(_connectionString))
            {
                var locationByQueue = LoadQueueLocations(conn);
                using (var cmd = OracleDb.CreateStoredProc(conn, "FQ_PROCS_GET.GET_APPT_DETAILS"))
                {
                    OracleDb.AddParam(cmd, "p_apptid", id, DbType.Int64);
                    OracleDb.AddOutRefCursor(cmd, "p_ref_cursor");
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read() ? MapAppointment(reader, locationByQueue) : null;
                    }
                }
            }
        }

        public void Add(Appointment appointment)
        {
            using (var conn = OracleDb.Open(_connectionString))
            {
                var newId = OracleDb.NextVal(conn, "APPTSEQ");
                appointment.Id = newId;

                var customerId = appointment.CustomerId;
                if (customerId <= 0)
                    throw new InvalidOperationException("CustomerId must be a numeric ID.");
                var queueId = appointment.QueueId;
                if (queueId <= 0)
                    throw new InvalidOperationException("QueueId must be a numeric ID.");

                var apptDate = ResolveApptDate(appointment);
                var startInterval = OracleInterval(appointment.StartTime ?? appointment.ScheduledForUtc.TimeOfDay);
                var endInterval = OracleInterval(appointment.EndTime);
                var createdBy = string.IsNullOrWhiteSpace(appointment.CreatedBy) ? "fastq" : appointment.CreatedBy;
                var stampUser = string.IsNullOrWhiteSpace(appointment.StampUser) ? "fastq" : appointment.StampUser;

                using (var cmd = OracleDb.CreateCommand(conn,
                    @"INSERT INTO APPOINTMENTS
                        (APPOINTMENT_ID, CUSTOMER_ID, QUEUE_ID, SERVICE_ID, REF_CRITERIA, REF_VALUE, CONTACTTYPE, MOREINFO,
                         APPT_DATE, START_TIME, END_TIME, STATUS, CONFCODE, MEETINGURL, LANGUAGE_PREF,
                         CREATEDBY, CREATEDON, STAMPUSER, STAMPDATE)
                      VALUES
                        (:apptId, :customerId, :queueId, :serviceId, :refCriteria, :refValue, :contactType, :moreInfo,
                         :apptDate, TO_DSINTERVAL(:startTime), TO_DSINTERVAL(:endTime), :status, :confCode, :meetingUrl, :languagePref,
                         :createdBy, SYSDATE, :stampUser, SYSDATE)"))
                {
                    OracleDb.AddParam(cmd, "apptId", newId, DbType.Int64);
                    OracleDb.AddParam(cmd, "customerId", customerId, DbType.Int64);
                    OracleDb.AddParam(cmd, "queueId", queueId, DbType.Int64);
                    OracleDb.AddParam(cmd, "serviceId", ToNullableLong(appointment.ServiceId), DbType.Int64);
                    OracleDb.AddParam(cmd, "refCriteria", appointment.RefCriteria, DbType.String);
                    OracleDb.AddParam(cmd, "refValue", appointment.RefValue, DbType.String);
                    OracleDb.AddParam(cmd, "contactType", appointment.ContactType, DbType.String);
                    OracleDb.AddParam(cmd, "moreInfo", appointment.MoreInfo, DbType.String);
                    OracleDb.AddParam(cmd, "status", appointment.Status.ToString(), DbType.String);
                    OracleDb.AddParam(cmd, "apptDate", apptDate, DbType.DateTime);
                    OracleDb.AddParam(cmd, "startTime", startInterval, DbType.String);
                    OracleDb.AddParam(cmd, "endTime", endInterval, DbType.String);
                    OracleDb.AddParam(cmd, "confCode", appointment.ConfirmationCode, DbType.String);
                    OracleDb.AddParam(cmd, "meetingUrl", appointment.MeetingUrl, DbType.String);
                    OracleDb.AddParam(cmd, "languagePref", appointment.LanguagePreference, DbType.String);
                    OracleDb.AddParam(cmd, "createdBy", createdBy, DbType.String);
                    OracleDb.AddParam(cmd, "stampUser", stampUser, DbType.String);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Appointment appointment)
        {
            var apptId = appointment.Id;
            if (apptId <= 0)
                throw new InvalidOperationException("Appointment Id must be a numeric ID.");
            var customerId = appointment.CustomerId;
            if (customerId <= 0)
                throw new InvalidOperationException("CustomerId must be a numeric ID.");
            var queueId = appointment.QueueId;
            if (queueId <= 0)
                throw new InvalidOperationException("QueueId must be a numeric ID.");

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"UPDATE APPOINTMENTS
                  SET CUSTOMER_ID = :customerId,
                      QUEUE_ID = :queueId,
                      SERVICE_ID = :serviceId,
                      REF_CRITERIA = :refCriteria,
                      REF_VALUE = :refValue,
                      CONTACTTYPE = :contactType,
                      MOREINFO = :moreInfo,
                      APPT_DATE = :apptDate,
                      START_TIME = TO_DSINTERVAL(:startTime),
                      END_TIME = TO_DSINTERVAL(:endTime),
                      STATUS = :status,
                      CONFCODE = :confCode,
                      MEETINGURL = :meetingUrl,
                      LANGUAGE_PREF = :languagePref,
                      STAMPUSER = :stampUser,
                      STAMPDATE = SYSDATE
                  WHERE APPOINTMENT_ID = :apptId"))
            {
                var apptDate = ResolveApptDate(appointment);
                var startInterval = OracleInterval(appointment.StartTime ?? appointment.ScheduledForUtc.TimeOfDay);
                var endInterval = OracleInterval(appointment.EndTime);
                var stampUser = string.IsNullOrWhiteSpace(appointment.StampUser) ? "fastq" : appointment.StampUser;

                OracleDb.AddParam(cmd, "customerId", customerId, DbType.Int64);
                OracleDb.AddParam(cmd, "queueId", queueId, DbType.Int64);
                OracleDb.AddParam(cmd, "serviceId", ToNullableLong(appointment.ServiceId), DbType.Int64);
                OracleDb.AddParam(cmd, "refCriteria", appointment.RefCriteria, DbType.String);
                OracleDb.AddParam(cmd, "refValue", appointment.RefValue, DbType.String);
                OracleDb.AddParam(cmd, "contactType", appointment.ContactType, DbType.String);
                OracleDb.AddParam(cmd, "moreInfo", appointment.MoreInfo, DbType.String);
                OracleDb.AddParam(cmd, "status", appointment.Status.ToString(), DbType.String);
                OracleDb.AddParam(cmd, "apptDate", apptDate, DbType.DateTime);
                OracleDb.AddParam(cmd, "startTime", startInterval, DbType.String);
                OracleDb.AddParam(cmd, "endTime", endInterval, DbType.String);
                OracleDb.AddParam(cmd, "confCode", appointment.ConfirmationCode, DbType.String);
                OracleDb.AddParam(cmd, "meetingUrl", appointment.MeetingUrl, DbType.String);
                OracleDb.AddParam(cmd, "languagePref", appointment.LanguagePreference, DbType.String);
                OracleDb.AddParam(cmd, "stampUser", stampUser, DbType.String);
                OracleDb.AddParam(cmd, "apptId", apptId, DbType.Int64);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Appointment> ListByQueue(long queueId)
        {
            if (queueId <= 0) return new List<Appointment>();
            return ListByFilter("a.QUEUE_ID = :queueId", cmd => OracleDb.AddParam(cmd, "queueId", queueId, DbType.Int64));
        }

        public IList<Appointment> ListByCustomer(long customerId)
        {
            if (customerId <= 0) return new List<Appointment>();
            return ListByFilter("a.CUSTOMER_ID = :customerId", cmd => OracleDb.AddParam(cmd, "customerId", customerId, DbType.Int64));
        }

        public IList<Appointment> ListByLocation(long locationId)
        {
            if (locationId <= 0) return new List<Appointment>();
            return ListByFilter("q.LOCATION_ID = :locationId", cmd => OracleDb.AddParam(cmd, "locationId", locationId, DbType.Int64));
        }

        public IList<Appointment> ListAll()
        {
            return ListByFilter(null, null);
        }

        public IList<ProviderAppointmentData> ListForUser(string userId, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            return ListForUserProc(userId, rangeStartUtc, rangeEndUtc, "FQ_PROCS_GET.GET_MYAPPOINTMENTS");
        }

        public IList<ProviderAppointmentData> ListWalkinsForUser(string userId, DateTime rangeStartUtc, DateTime rangeEndUtc)
        {
            return ListForUserProc(userId, rangeStartUtc, rangeEndUtc, "FQ_PROCS_GET.GET_MYWALKINS");
        }

        private IList<ProviderAppointmentData> ListForUserProc(string userId, DateTime rangeStartUtc, DateTime rangeEndUtc, string procName)
        {
            var list = new List<ProviderAppointmentData>();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return list;
            }

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateStoredProc(conn, procName))
            {
                OracleDb.AddParam(cmd, "p_userid", userId.Trim(), DbType.String);
                OracleDb.AddParam(cmd, "p_range_startdate", rangeStartUtc.Date, DbType.DateTime);
                OracleDb.AddParam(cmd, "p_range_enddate", rangeEndUtc.Date, DbType.DateTime);
                OracleDb.AddOutRefCursor(cmd, "p_cur");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var statusText = ReadField(reader, "STATUS");
                        var status = MapStatus(statusText);

                        var isWalkin = HasField(reader, "WALKIN_ID") || HasField(reader, "JOIN_TIME");
                        long apptId;
                        if (isWalkin && TryGetLong(reader, "WALKIN_ID", out var walkinId))
                        {
                            apptId = walkinId;
                        }
                        else
                        {
                            apptId = Convert.ToInt64(reader["APPOINTMENT_ID"]);
                        }

                        DateTime scheduled;
                        if (isWalkin)
                        {
                            var joinTime = ReadDateTime(reader, "JOIN_TIME");
                            var createdOn = ReadDateTime(reader, "CREATEDON");
                            var baseTime = joinTime ?? createdOn ?? DateTime.UtcNow;
                            scheduled = DateTime.SpecifyKind(baseTime, DateTimeKind.Utc);
                        }
                        else
                        {
                            var apptDate = reader["APPT_DATE"] == DBNull.Value
                                ? DateTime.UtcNow
                                : Convert.ToDateTime(reader["APPT_DATE"]);
                            var startTime = ReadInterval(reader, "START_TIME");
                            scheduled = DateTime.SpecifyKind(apptDate, DateTimeKind.Utc);
                            if (startTime.HasValue)
                            {
                                scheduled = DateTime.SpecifyKind(apptDate.Date + startTime.Value, DateTimeKind.Utc);
                            }
                        }

                        var first = ReadField(reader, "CUST_FNAME");
                        var last = ReadField(reader, "CUST_LNAME");
                        var fullName = string.Join(" ", new[] { first, last }.Where(v => !string.IsNullOrWhiteSpace(v)));

                        list.Add(new ProviderAppointmentData
                        {
                            AppointmentId = apptId,
                            ScheduledForUtc = scheduled,
                            Status = status,
                            QueueName = ReadField(reader, "NAME"),
                            ServiceName = ReadField(reader, "SERVICE_NAME"),
                            CustomerName = fullName,
                            CustomerPhone = ReadField(reader, "CUST_PHONE"),
                            SmsOptIn = string.Equals(ReadField(reader, "SMS_OPTIN"), "Y", StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }
            }

            return list;
        }

        public void UpdateStatus(long appointmentId, string status, string stampUser, string notes = null)
        {
            var apptId = appointmentId;
            if (apptId <= 0)
                throw new InvalidOperationException("Appointment Id must be a numeric ID.");

            if (string.IsNullOrWhiteSpace(status))
                throw new InvalidOperationException("Status is required.");

            var user = string.IsNullOrWhiteSpace(stampUser) ? "fastq" : stampUser.Trim();

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateStoredProc(conn, "FQ_PROCS_IDU.UPDATE_APPT_STATUS"))
            {
                OracleDb.AddParam(cmd, "p_apptid", apptId, DbType.Int64);
                OracleDb.AddParam(cmd, "p_status", status.Trim(), DbType.String);
                OracleDb.AddParam(cmd, "p_stampuser", user, DbType.String);
                OracleDb.AddParam(cmd, "p_notes", notes, DbType.String);

                var outMsg = cmd.CreateParameter();
                outMsg.ParameterName = "p_outmsg";
                outMsg.Direction = ParameterDirection.Output;
                outMsg.DbType = DbType.String;
                outMsg.Size = 4000;
                cmd.Parameters.Add(outMsg);

                cmd.ExecuteNonQuery();

                var message = outMsg.Value == DBNull.Value ? string.Empty : outMsg.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(message))
                    throw new InvalidOperationException(message);
            }
        }


        private IList<Appointment> ListByFilter(string whereClause, Action<DbCommand> addParams)
        {
            var list = new List<Appointment>();
            var sql = @"SELECT a.*, q.LOCATION_ID
                        FROM APPOINTMENTS a
                        JOIN VALIDQUEUES q ON q.QUEUE_ID = a.QUEUE_ID";
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                sql += " WHERE " + whereClause;
            }

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn, sql))
            {
                addParams?.Invoke(cmd);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapAppointment(reader, null));
                    }
                }
            }

            return list;
        }

        private static string ReadField(IDataRecord record, string field)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                if (!string.Equals(record.GetName(i), field, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (record.IsDBNull(i))
                {
                    return string.Empty;
                }

                return record.GetValue(i)?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static bool HasField(IDataRecord record, string field)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                if (string.Equals(record.GetName(i), field, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static Appointment MapAppointment(IDataRecord record, IDictionary<long, long> locationByQueue)
        {
            var apptId = Convert.ToInt64(record["APPOINTMENT_ID"]);
            var customerId = Convert.ToInt64(record["CUSTOMER_ID"]);
            var queueId = Convert.ToInt64(record["QUEUE_ID"]);
            var locationId = ResolveLocationId(record, queueId, locationByQueue);
            var apptDate = record["APPT_DATE"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(record["APPT_DATE"]);
            var startTime = ReadInterval(record, "START_TIME");
            var endTime = ReadInterval(record, "END_TIME");
            var createdOn = record["CREATEDON"] == DBNull.Value ? apptDate : Convert.ToDateTime(record["CREATEDON"]);
            var stampDate = record["STAMPDATE"] == DBNull.Value ? createdOn : Convert.ToDateTime(record["STAMPDATE"]);
            var statusText = record["STATUS"] == DBNull.Value ? AppointmentStatus.Scheduled.ToString() : record["STATUS"].ToString();
            var serviceId = TryReadLong(record, "SERVICE_ID");

            var status = MapStatus(statusText);

            return new Appointment
            {
                Id = apptId,
                CustomerId = customerId,
                QueueId = queueId,
                LocationId = locationId,
                ServiceId = serviceId,
                RefCriteria = record["REF_CRITERIA"]?.ToString(),
                RefValue = record["REF_VALUE"]?.ToString(),
                ContactType = record["CONTACTTYPE"]?.ToString(),
                MoreInfo = record["MOREINFO"]?.ToString(),
                ApptDateUtc = DateTime.SpecifyKind(apptDate, DateTimeKind.Utc),
                StartTime = startTime,
                EndTime = endTime,
                Status = status,
                ConfirmationCode = record["CONFCODE"]?.ToString(),
                MeetingUrl = record["MEETINGURL"]?.ToString(),
                LanguagePreference = record["LANGUAGE_PREF"]?.ToString(),
                CreatedBy = record["CREATEDBY"]?.ToString(),
                CreatedOnUtc = DateTime.SpecifyKind(createdOn, DateTimeKind.Utc),
                StampUser = record["STAMPUSER"]?.ToString(),
                StampDateUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Utc),
                CreatedUtc = DateTime.SpecifyKind(createdOn, DateTimeKind.Utc),
                UpdatedUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Utc)
            };
        }

        private static long ResolveLocationId(IDataRecord record, long queueId, IDictionary<long, long> locationByQueue)
        {
            if (TryGetLong(record, "LOCATION_ID", out var locationId))
            {
                return locationId;
            }

            if (locationByQueue != null && locationByQueue.TryGetValue(queueId, out var mappedId))
            {
                return mappedId;
            }

            return 0;
        }

        private static bool TryGetLong(IDataRecord record, string field, out long value)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                if (!string.Equals(record.GetName(i), field, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (record.IsDBNull(i))
                {
                    value = 0;
                    return false;
                }

                value = Convert.ToInt64(record.GetValue(i));
                return true;
            }

            value = 0;
            return false;
        }

        private static long? TryReadLong(IDataRecord record, string field)
        {
            return TryGetLong(record, field, out var value) ? value : (long?)null;
        }

        private static DateTime? ReadDateTime(IDataRecord record, string field)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                if (!string.Equals(record.GetName(i), field, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (record.IsDBNull(i))
                {
                    return null;
                }

                var value = record.GetValue(i);
                if (value is DateTime dt)
                {
                    return dt;
                }

                if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return parsed;
                }

                return null;
            }

            return null;
        }

        private static TimeSpan? ReadInterval(IDataRecord record, string field)
        {
            for (var i = 0; i < record.FieldCount; i++)
            {
                if (!string.Equals(record.GetName(i), field, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (record.IsDBNull(i))
                {
                    return null;
                }

                var value = record.GetValue(i);
                if (value is TimeSpan ts)
                {
                    return ts;
                }

                var text = value.ToString();
                return ParseInterval(text);
            }

            return null;
        }

        private static TimeSpan? ParseInterval(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var trimmed = text.Trim();
            var negative = trimmed.StartsWith("-", StringComparison.Ordinal);
            trimmed = trimmed.TrimStart('+', '-');

            var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var days = 0;
            var timePart = parts.Length == 2 ? parts[1] : parts[0];
            if (parts.Length == 2 && !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out days))
            {
                days = 0;
            }

            if (!TimeSpan.TryParse(timePart, CultureInfo.InvariantCulture, out var time))
            {
                if (!TimeSpan.TryParseExact(timePart, "hh\\:mm\\:ss\\.FFFFFF", CultureInfo.InvariantCulture, out time))
                {
                    return null;
                }
            }

            var result = time.Add(TimeSpan.FromDays(days));
            return negative ? -result : result;
        }

        private static DateTime ResolveApptDate(Appointment appointment)
        {
            if (appointment.ApptDateUtc != default)
            {
                return appointment.ApptDateUtc;
            }

            return appointment.ScheduledForUtc;
        }

        private static AppointmentStatus MapStatus(string statusText)
        {
            if (string.IsNullOrWhiteSpace(statusText))
                return AppointmentStatus.Scheduled;

            var trimmed = statusText.Trim();
            if (trimmed.Equals("ARRIVED", StringComparison.OrdinalIgnoreCase))
                return AppointmentStatus.Arrived;
            if (trimmed.Equals("IN PROGRESS", StringComparison.OrdinalIgnoreCase))
                return AppointmentStatus.InService;
            if (trimmed.Equals("DONE", StringComparison.OrdinalIgnoreCase))
                return AppointmentStatus.Completed;
            if (trimmed.Equals("REMOVED", StringComparison.OrdinalIgnoreCase))
                return AppointmentStatus.Cancelled;
            if (trimmed.Equals("CANCELED", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase))
                return AppointmentStatus.Cancelled;
            if (trimmed.Equals("REJOINED", StringComparison.OrdinalIgnoreCase))
                return AppointmentStatus.Arrived;
            if (trimmed.Equals("QUEUED", StringComparison.OrdinalIgnoreCase))
                return AppointmentStatus.Arrived;
            if (trimmed.Equals("STARTED", StringComparison.OrdinalIgnoreCase))
                return AppointmentStatus.InService;
            if (trimmed.Equals("Transfered", StringComparison.OrdinalIgnoreCase))
                return AppointmentStatus.TransferredOut;
            if (trimmed.Equals("REMOVED", StringComparison.OrdinalIgnoreCase))
                return AppointmentStatus.Cancelled;

            return Enum.TryParse(trimmed, true, out AppointmentStatus parsed)
                ? parsed
                : AppointmentStatus.Scheduled;
        }

        private static string OracleInterval(TimeSpan? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            var ts = value.Value;
            var days = Math.Abs(ts.Days);
            var time = new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds);
            var sign = ts < TimeSpan.Zero ? "-" : "+";
            return string.Format(CultureInfo.InvariantCulture, "{0}{1:00} {2:hh\\:mm\\:ss}.000000", sign, days, time);
        }

        private static object ToNullableLong(long? id)
        {
            if (!id.HasValue || id.Value <= 0)
            {
                return DBNull.Value;
            }

            return id.Value;
        }

        private static Dictionary<long, long> LoadQueueLocations(DbConnection conn)
        {
            var map = new Dictionary<long, long>();
            using (var cmd = OracleDb.CreateStoredProc(conn, "FQ_PROCS_GET.GET_QUEUES"))
            {
                OracleDb.AddParam(cmd, "p_location", null, DbType.Int64);
                OracleDb.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var queueId = Convert.ToInt64(reader["QUEUE_ID"]);
                        var locationId = Convert.ToInt64(reader["LOCATION_ID"]);
                        map[queueId] = locationId;
                    }
                }
            }

            return map;
        }
    }
}

