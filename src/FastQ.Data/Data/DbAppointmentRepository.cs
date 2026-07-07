using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using Newtonsoft.Json.Linq;

namespace FastQ.Data.Db
{
    public sealed class DbAppointmentRepository : IAppointmentRepository
    {
        public Appointment Get(long id)
        {
            if (id <= 0) return null;

            using (var conn = DataAccess.Open())
            {
                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_APPT_DETAILS"))
                {
                    DataAccess.AddParam(cmd, "p_apptid", id, DbType.Int64);
                    DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                    using (var reader = cmd.ExecuteReader())
                    {
                        return reader.Read() ? MapAppointment(reader) : null;
                    }
                }
            }
        }

        public void Add(Appointment appointment)
        {
            using (var conn = DataAccess.Open())
            {
                var queueId = appointment.QueueId;
                if (queueId <= 0)
                    throw new InvalidOperationException("QueueId must be a numeric ID.");
                var customerEmail = (appointment.CustomerEmail ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(customerEmail))
                    throw new InvalidOperationException("CustomerEmail is required for FQ_EXTERNAL.INSERT_APPT.");

                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_EXTERNAL.INSERT_APPT"))
                {
                    DataAccess.AddParam(cmd, "p_email", customerEmail, DbType.String);
                    DataAccess.AddParam(cmd, "p_json", BuildInsertApptPayload(appointment), DbType.String);
                    DataAccess.AddParam(cmd, "p_stampuser", string.IsNullOrWhiteSpace(appointment.StampUser) ? "web" : appointment.StampUser.Trim(), DbType.String);

                    var confCode = DataAccess.AddParam(cmd, "p_confcode", null, DbType.String);
                    confCode.Direction = ParameterDirection.Output;
                    confCode.Size = 32;

                    var outMsg = DataAccess.AddParam(cmd, "p_out", null, DbType.String);
                    outMsg.Direction = ParameterDirection.Output;
                    outMsg.Size = 4000;

                    cmd.ExecuteNonQuery();

                    var error = outMsg.Value == DBNull.Value ? string.Empty : outMsg.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(error))
                        throw new InvalidOperationException(error);

                    appointment.ConfirmationCode = confCode.Value == DBNull.Value ? null : confCode.Value?.ToString();
                    appointment.Id = LookupAppointmentIdByConfirmationCode(conn, appointment.ConfirmationCode);
                    if (appointment.Id <= 0)
                        throw new InvalidOperationException("FQ_EXTERNAL.INSERT_APPT did not return a resolvable appointment id.");
                }
            }
        }

        public long AddWalkin(Appointment appointment)
        {
            using (var conn = DataAccess.Open())
            {
                long newId = 0;      //DataAccess.NextVal(conn, "WALKINSEQ");
                appointment.Id = newId;

                var customerId = appointment.CustomerId;
                if (customerId <= 0)
                    throw new InvalidOperationException("CustomerId must be a numeric ID.");
                var queueId = appointment.QueueId;
                if (queueId <= 0)
                    throw new InvalidOperationException("QueueId must be a numeric ID.");

                var joinInterval = OracleInterval(appointment.StartTime ?? appointment.ScheduledFor.TimeOfDay);
                var endInterval = OracleInterval(appointment.EndTime);
                var createdBy = string.IsNullOrWhiteSpace(appointment.CreatedBy) ? "fastq" : appointment.CreatedBy;
                var stampUser = string.IsNullOrWhiteSpace(appointment.StampUser) ? "fastq" : appointment.StampUser;

                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS.INSERT_WALKIN"))
                {
                    DataAccess.AddParam(cmd, "p_customer_id", customerId, DbType.Int64);
                    DataAccess.AddParam(cmd, "p_queue_id", queueId, DbType.Int64);
                    DataAccess.AddParam(cmd, "p_service_id", ToNullableLong(appointment.ServiceId), DbType.Int64);
                    DataAccess.AddParam(cmd, "p_ref_criteria", appointment.RefCriteria, DbType.String);
                    DataAccess.AddParam(cmd, "p_ref_value", appointment.RefValue, DbType.String);
                    DataAccess.AddParam(cmd, "p_contacttype", appointment.ContactType, DbType.String);
                    DataAccess.AddParam(cmd, "p_moreinfo", appointment.MoreInfo, DbType.String);
                    DataAccess.AddParam(cmd, "p_join_time", joinInterval, DbType.String);
                    DataAccess.AddParam(cmd, "p_end_time", endInterval, DbType.String);
                    DataAccess.AddParam(cmd, "p_status", appointment.Status.ToString(), DbType.String);
                    DataAccess.AddParam(cmd, "p_meetingurl", appointment.MeetingUrl, DbType.String);
                    DataAccess.AddParam(cmd, "p_language_pref", appointment.LanguagePreference, DbType.String);
                    DataAccess.AddParam(cmd, "p_createdby", createdBy, DbType.String);
                    DataAccess.AddParam(cmd, "p_stampuser", stampUser, DbType.String);
                    var outId = DataAccess.AddParam(cmd, "p_walkin_id", null, DbType.Int64);
                    outId.Direction = ParameterDirection.Output;
                    var outMsg = DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String);
                    outMsg.Direction = ParameterDirection.Output;
                    outMsg.Size = 4000;
                    cmd.ExecuteNonQuery();
                    var error = outMsg.Value == DBNull.Value ? string.Empty : outMsg.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(error))
                        throw new InvalidOperationException(error);
                    if (outId.Value != DBNull.Value && outId.Value != null)
                        newId = Convert.ToInt64(outId.Value);
                }

                return newId;
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

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS.UPDATE_APPOINTMENT"))
            {
                var apptDate = ResolveApptDate(appointment);
                var startInterval = OracleInterval(appointment.StartTime ?? appointment.ScheduledFor.TimeOfDay);
                var endInterval = OracleInterval(appointment.EndTime);
                var stampUser = string.IsNullOrWhiteSpace(appointment.StampUser) ? "fastq" : appointment.StampUser;

                DataAccess.AddParam(cmd, "p_apptid", apptId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_customer_id", customerId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_queue_id", queueId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_service_id", ToNullableLong(appointment.ServiceId), DbType.Int64);
                DataAccess.AddParam(cmd, "p_ref_criteria", appointment.RefCriteria, DbType.String);
                DataAccess.AddParam(cmd, "p_ref_value", appointment.RefValue, DbType.String);
                DataAccess.AddParam(cmd, "p_contacttype", appointment.ContactType, DbType.String);
                DataAccess.AddParam(cmd, "p_moreinfo", appointment.MoreInfo, DbType.String);
                DataAccess.AddParam(cmd, "p_appt_date", apptDate, DbType.DateTime);
                DataAccess.AddParam(cmd, "p_start_time", startInterval, DbType.String);
                DataAccess.AddParam(cmd, "p_end_time", endInterval, DbType.String);
                DataAccess.AddParam(cmd, "p_status", appointment.Status.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_confcode", appointment.ConfirmationCode, DbType.String);
                DataAccess.AddParam(cmd, "p_meetingurl", appointment.MeetingUrl, DbType.String);
                DataAccess.AddParam(cmd, "p_language_pref", appointment.LanguagePreference, DbType.String);
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

        public IList<Appointment> ListByQueue(long queueId)
        {
            if (queueId <= 0) return new List<Appointment>();
            return ListByFilter("a.QUEUE_ID = :queueId", cmd => DataAccess.AddParam(cmd, "queueId", queueId, DbType.Int64));
        }

        public IList<Appointment> ListByCustomer(long customerId)
        {
            if (customerId <= 0) return new List<Appointment>();
            return ListByFilter("a.CUSTOMER_ID = :customerId", cmd => DataAccess.AddParam(cmd, "customerId", customerId, DbType.Int64));
        }

        public IList<Appointment> ListByEntity(long entityId)
        {
            if (entityId <= 0) return new List<Appointment>();
            return ListByFilter("q.ENTITY_ID = :entityId", cmd => DataAccess.AddParam(cmd, "entityId", entityId, DbType.Int64));
        }

        //public IList<Appointment> ListAll()
        //{
        //    return ListByFilter(null, null);
        //}

        public IList<QueueOpenSlot> GetQueueOpenSlots(long queueId, DateTime dateLocal)
        {
            var list = new List<QueueOpenSlot>();
            if (queueId <= 0)
            {
                return list;
            }

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateCommand(conn, "BEGIN fqowner.FQ_EXTERNAL.GET_QUEUE_OPENSLOTS(:p_queueid, :p_thedate, :p_json); END;"))
            {
                DataAccess.AddParam(cmd, "p_queueid", queueId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_thedate", dateLocal.Date, DbType.DateTime);
                var jsonParam = DataAccess.AddParam(cmd, "p_json", null, DbType.String);
                jsonParam.Direction = ParameterDirection.Output;
                jsonParam.Size = 32767;

                cmd.ExecuteNonQuery();

                var rawJson = jsonParam.Value == DBNull.Value ? string.Empty : jsonParam.Value?.ToString();
                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    return list;
                }

                var array = JArray.Parse(rawJson);
                foreach (var item in array.OfType<JObject>())
                {
                    DateTime parsedDate;
                    DateTime.TryParse(item["thedate"]?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
                    list.Add(new QueueOpenSlot
                    {
                        TheDate = parsedDate == default ? dateLocal.Date : parsedDate.Date,
                        QueueId = item["queue_id"]?.ToObject<long?>() ?? queueId,
                        SlotBegin = item["slot_begin"]?.ToString() ?? string.Empty,
                        SlotEnd = item["slot_end"]?.ToString() ?? string.Empty,
                        WeeklySchedule = item["weekly_sch"]?.ToString() ?? string.Empty,
                        IntervalTime = item["interval_time"]?.ToString() ?? string.Empty,
                        AvailableResources = item["available_resources"]?.ToObject<int?>() ?? 0
                    });
                }
            }

            return list;
        }

        public IList<ProviderAppointmentData> ListForUser(long entityId, string userId, DateTime rangeStart, DateTime rangeEnd)
        {
            return ListForUserProc(entityId, userId, rangeStart, rangeEnd, "fqowner.FQ_PROCS_GET.GET_MYAPPOINTMENTS");
        }

        public IList<ProviderAppointmentData> ListWalkinsForUser(long entityId, string userId, DateTime rangeStart, DateTime rangeEnd)
        {
            return ListForUserProc(entityId, userId, rangeStart, rangeEnd, "fqowner.FQ_PROCS_GET.GET_MYWALKINS");
        }

        public bool ValidatePermitNumber(long queueId, string permitNumber, out string message)
        {
            message = string.Empty;

            if (queueId <= 0)
            {
                message = "Queue is required.";
                return false;
            }

            var normalizedPermit = (permitNumber ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedPermit))
            {
                message = "Permit number is required.";
                return false;
            }

            try
            {
                using (var conn = DataAccess.Open())
                using (var cmd = DataAccess.CreateCommand(conn,
                    "BEGIN fqowner.FQ_PROCS.VALIDATE_PERMIT(:p_queueid, :p_permit_number, :p_is_valid, :p_outmsg); END;"))
                {
                    DataAccess.AddParam(cmd, "p_queueid", queueId, DbType.Int64);
                    DataAccess.AddParam(cmd, "p_permit_number", normalizedPermit, DbType.String);

                    var validParam = DataAccess.AddParam(cmd, "p_is_valid", null, DbType.Int32);
                    validParam.Direction = ParameterDirection.Output;

                    var outMsg = DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String);
                    outMsg.Direction = ParameterDirection.Output;
                    outMsg.Size = 4000;

                    cmd.ExecuteNonQuery();

                    var isValid = ToOracleBool(validParam.Value);
                    var dbMessage = outMsg.Value == DBNull.Value ? string.Empty : outMsg.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(dbMessage))
                    {
                        message = dbMessage.Trim();
                    }

                    if (!isValid && string.IsNullOrWhiteSpace(message))
                    {
                        message = "Permit number is invalid.";
                    }

                    return isValid;
                }
            }
            catch (Exception ex)
            {
                message = "Permit validation failed: " + ex.Message;
                return false;
            }
        }

        private IList<ProviderAppointmentData> ListForUserProc(long entityId, string userId, DateTime rangeStart, DateTime rangeEnd, string procName)
        {
            var list = new List<ProviderAppointmentData>();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return list;
            }

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, procName))
            {
                DataAccess.AddParam(cmd, "p_entityid", entityId, DbType.Int64);
                DataAccess.AddParam(cmd, "p_userid", userId.Trim(), DbType.String);
                DataAccess.AddParam(cmd, "p_range_startdate", rangeStart.Date, DbType.DateTime);
                DataAccess.AddParam(cmd, "p_range_enddate", rangeEnd.Date, DbType.DateTime);
                DataAccess.AddOutRefCursor(cmd, "p_cur");
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
                            var stampDate = ReadDateTime(reader, "STAMPDATE");
                            scheduled = DateTime.SpecifyKind(
                                ComposeWalkinScheduledTime(joinTime, createdOn, stampDate),
                                DateTimeKind.Local);
                        }
                        else
                        {
                            var apptDate = reader["APPT_DATE"] == DBNull.Value
                                ? DateTime.Now
                                : Convert.ToDateTime(reader["APPT_DATE"]);
                            var startTime = ReadInterval(reader, "START_TIME");
                            scheduled = DateTime.SpecifyKind(apptDate, DateTimeKind.Local);
                            if (startTime.HasValue)
                            {
                                scheduled = DateTime.SpecifyKind(apptDate.Date + startTime.Value, DateTimeKind.Local);
                            }
                        }

                        var first = ReadField(reader, "CUST_FNAME");
                        var last = ReadField(reader, "CUST_LNAME");
                        var fullName = string.Join(" ", new[] { first, last }.Where(v => !string.IsNullOrWhiteSpace(v)));

                        list.Add(new ProviderAppointmentData
                        {
                            AppointmentId = apptId,
                            QueueId = TryGetLong(reader, "QUEUE_ID", out var queueId) ? queueId : 0,
                            ServiceId = TryGetLong(reader, "SERVICE_ID", out var serviceId) ? serviceId : 0,
                            ScheduledFor = scheduled,
                            Status = status,
                            QueueName = ReadField(reader, "NAME"),
                            ServiceName = ReadField(reader, "SERVICE_NAME"),
                            CustomerName = fullName,
                            CustomerEmail = ReadField(reader, "CUST_EMAIL"),
                            CustomerPhone = ReadField(reader, "CUST_PHONE"),
                            ContactType = ReadField(reader, "CONTACTTYPE"),
                            RefValue = ReadField(reader, "REF_VALUE"),
                            LanguagePreference = ReadField(reader, "LANGUAGE_PREF"),
                            MeetingUrl = ReadMeetingUrl(reader),
                            MeetingUrlHost = ReadMeetingUrlHost(reader),
                            Notes = ReadField(reader, "MOREINFO"),
                            StampUser = ReadField(reader, "STAMPUSER"),
                            StampUserName = ReadField(reader, "STAMPUSERNAME"),
                            SmsOptIn = string.Equals(ReadField(reader, "SMS_OPTIN"), "Y", StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }
            }

            return list;
        }

        public long? GetQueueIdForSource(char srcType, long sourceId)
        {
            if (sourceId <= 0)
            {
                return null;
            }

            var normalizedSrcType = char.ToUpperInvariant(srcType);

            using (var conn = DataAccess.Open())
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_QUEUE_ID_FOR_SOURCE"))
            {
                DataAccess.AddParam(cmd, "p_src_type", normalizedSrcType.ToString(), DbType.String);
                DataAccess.AddParam(cmd, "p_src_id", sourceId, DbType.Int64);
                var outParam = DataAccess.AddParam(cmd, "p_queue_id", null, DbType.Int64);
                outParam.Direction = ParameterDirection.Output;
                cmd.ExecuteNonQuery();
                var value = outParam.Value;
                if (value == null || value == DBNull.Value)
                {
                    return null;
                }

                return Convert.ToInt64(value);
            }
        }

        private IList<Appointment> ListByFilter(string whereClause, Action<DbCommand> addParams)
        {
            var list = new List<Appointment>();
            using (var conn = DataAccess.Open())
            using (var cmd = CreateListCommand(conn, whereClause, addParams))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapAppointment(reader));
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

        private static string ReadMeetingUrl(IDataRecord record)
        {
            return ReadField(record, "MEETINGURL");
        }

        private static string ReadMeetingUrlHost(IDataRecord record)
        {
            return ReadField(record, "MEETINGURL_HOST");
        }

        private static Appointment MapAppointment(IDataRecord record)
        {
            var apptId = Convert.ToInt64(record["APPOINTMENT_ID"]);
            var customerId = Convert.ToInt64(record["CUSTOMER_ID"]);
            var queueId = Convert.ToInt64(record["QUEUE_ID"]);
            var entityId = Convert.ToInt64(record["ENTITY_ID"]);
            var apptDate = record["APPT_DATE"] == DBNull.Value ? default : Convert.ToDateTime(record["APPT_DATE"]);
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
                CustomerEmail = ReadField(record, "EMAIL"),
                CustomerFirstName = ReadField(record, "FNAME"),
                CustomerLastName = ReadField(record, "LNAME"),
                CustomerPhone = ReadField(record, "PHONE"),
                QueueId = queueId,
                EntityId = entityId,
                ServiceId = serviceId,
                RefCriteria = record["REF_CRITERIA"]?.ToString(),
                RefValue = record["REF_VALUE"]?.ToString(),
                ContactType = record["CONTACTTYPE"]?.ToString(),
                MoreInfo = record["MOREINFO"]?.ToString(),
                ApptDate = apptDate,
                StartTime = startTime,
                EndTime = endTime,
                Status = status,
                ConfirmationCode = record["CONFCODE"]?.ToString(),
                MeetingUrl = ReadMeetingUrl(record),
                MeetingUrlHost = ReadMeetingUrlHost(record),
                LanguagePreference = record["LANGUAGE_PREF"]?.ToString(),
                CreatedBy = record["CREATEDBY"]?.ToString(),
                CreatedOn = createdOn,
                StampUser = record["STAMPUSER"]?.ToString(),
                StampDate = stampDate,
                //CreatedOn = createdOn,
                UpdatedOn = stampDate
            };
        }

        private static long ResolveEntityId(IDataRecord record, long queueId, IDictionary<long, long> entityByQueue)
        {
            if (TryGetLong(record, "ENTITY_ID", out var entityId))
            {
                return entityId;
            }

            if (entityByQueue != null && entityByQueue.TryGetValue(queueId, out var mappedId))
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

        private static DateTime ComposeWalkinScheduledTime(DateTime? joinTime, DateTime? createdOn, DateTime? stampDate)
        {
            if (joinTime.HasValue && createdOn.HasValue && joinTime.Value.Date != createdOn.Value.Date)
            {
                return createdOn.Value.Date + joinTime.Value.TimeOfDay;
            }

            if (joinTime.HasValue && HasTimeOfDay(joinTime.Value))
            {
                return joinTime.Value;
            }

            if (createdOn.HasValue)
            {
                var created = createdOn.Value;
                if (joinTime.HasValue && HasTimeOfDay(joinTime.Value))
                {
                    return created.Date + joinTime.Value.TimeOfDay;
                }

                if (stampDate.HasValue && HasTimeOfDay(stampDate.Value) && !HasTimeOfDay(created))
                {
                    return created.Date + stampDate.Value.TimeOfDay;
                }

                return created;
            }

            if (stampDate.HasValue)
            {
                return stampDate.Value;
            }

            return DateTime.Now;
        }

        private static bool HasTimeOfDay(DateTime value)
        {
            return value.TimeOfDay != TimeSpan.Zero;
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

        private static string BuildInsertApptPayload(Appointment appointment)
        {
            var firstName = (appointment.CustomerFirstName ?? string.Empty).Trim();
            var lastName = (appointment.CustomerLastName ?? string.Empty).Trim();
            var customerEmail = (appointment.CustomerEmail ?? string.Empty).Trim().ToLowerInvariant();
            var customerPhone = (appointment.CustomerPhone ?? string.Empty).Trim();

            var payload = new JArray(
                new JObject
                {
                    ["REF_CRITERIA"] = appointment.RefCriteria,
                    ["REF_VALUE"] = appointment.RefValue,
                    ["QUEUE_ID"] = appointment.QueueId,
                    ["SERVICE_ID"] = appointment.ServiceId.HasValue && appointment.ServiceId.Value > 0
                        ? (JToken)new JValue(appointment.ServiceId.Value)
                        : JValue.CreateNull(),
                    ["CONTACTTYPE"] = appointment.ContactType,
                    ["MEETINGURL"] = string.IsNullOrWhiteSpace(appointment.MeetingUrl)
                        ? JValue.CreateNull()
                        : (JToken)new JValue(appointment.MeetingUrl.Trim()),
                    ["MOREINFO"] = appointment.MoreInfo,
                    ["APPT_DATE"] = ResolveApptDate(appointment).ToString("dd-MMM-yy", CultureInfo.InvariantCulture).ToUpperInvariant(),
                    ["START_TIME"] = OracleInterval(appointment.StartTime ?? appointment.ScheduledFor.TimeOfDay),
                    ["END_TIME"] = OracleInterval(appointment.EndTime),
                    ["STATUS"] = appointment.Status.ToString().ToUpperInvariant(),
                    ["LANGUAGE_PREF"] = appointment.LanguagePreference,
                    ["CREATEDBY"] = string.IsNullOrWhiteSpace(appointment.CreatedBy)
                        ? JValue.CreateNull()
                        : (JToken)new JValue(appointment.CreatedBy.Trim()),
                    ["STAMPUSER"] = string.IsNullOrWhiteSpace(appointment.StampUser)
                        ? JValue.CreateNull()
                        : (JToken)new JValue(appointment.StampUser.Trim()),
                    ["FNAME"] = firstName,
                    ["LNAME"] = lastName,
                    ["EMAIL"] = customerEmail,
                    ["PHONE"] = customerPhone,
                    ["SMSOPTIN"] = appointment.CustomerSmsOptIn ? "Y" : "N"
                });

            return payload.ToString();
        }

        private static long LookupAppointmentIdByConfirmationCode(DbConnection conn, string confirmationCode)
        {
            if (string.IsNullOrWhiteSpace(confirmationCode))
            {
                return 0;
            }

            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_APPT_ID_BY_CONF"))
            {
                DataAccess.AddParam(cmd, "p_confcode", confirmationCode.Trim(), DbType.String);
                var outParam = DataAccess.AddParam(cmd, "p_apptid", null, DbType.Int64);
                outParam.Direction = ParameterDirection.Output;
                cmd.ExecuteNonQuery();
                var value = outParam.Value;
                if (value == null || value == DBNull.Value)
                {
                    return 0;
                }

                return Convert.ToInt64(value, CultureInfo.InvariantCulture);
            }
        }

        private static DateTime ResolveApptDate(Appointment appointment)
        {
            if (appointment.ApptDate != default)
            {
                return appointment.ApptDate;
            }

            return appointment.ScheduledFor;
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
            if (trimmed.Equals("TRANSFERRED", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("TRANSFERED", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("TRANSFERRED OUT", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Transfered", StringComparison.OrdinalIgnoreCase)
                || trimmed.Equals("Transferred", StringComparison.OrdinalIgnoreCase))
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

        private static Dictionary<long, long> LoadQueueEntities(DbConnection conn)
        {
            var map = new Dictionary<long, long>();
            using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_QUEUES"))
            {
                DataAccess.AddParam(cmd, "p_entity_id", null, DbType.Int64);
                DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var queueId = Convert.ToInt64(reader["QUEUE_ID"]);
                        var entityId = Convert.ToInt64(reader["ENTITY_ID"]);
                        map[queueId] = entityId;
                    }
                }
            }
            return map;
        }

        private static bool IsEntityFilter(string whereClause)
        {
            return string.Equals(whereClause, "q.ENTITY_ID = :entityId", StringComparison.OrdinalIgnoreCase)
                || string.Equals(whereClause, "q.ENTITY_ID = :entityId", StringComparison.OrdinalIgnoreCase);
        }

        private static DbCommand CreateListCommand(DbConnection conn, string whereClause, Action<DbCommand> addParams)
        {
            DbCommand cmd;
            if (string.Equals(whereClause, "a.QUEUE_ID = :queueId", StringComparison.OrdinalIgnoreCase))
            {
                cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_APPTS_BY_QUEUE");
            }
            else if (string.Equals(whereClause, "a.CUSTOMER_ID = :customerId", StringComparison.OrdinalIgnoreCase))
            {
                cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_APPTS_BY_CUSTOMER");
            }
            else if (IsEntityFilter(whereClause))
            {
                cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_APPTS_BY_ENTITY");
            }
            else
            {
                cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_ALL_APPTS");
            }

            addParams?.Invoke(cmd);
            RenameParameterIfPresent(cmd, "queueId", "p_queueid");
            RenameParameterIfPresent(cmd, "customerId", "p_customerid");
            RenameParameterIfPresent(cmd, "entityId", "p_entityid");
            DataAccess.AddOutRefCursor(cmd, "p_cur");
            return cmd;
        }

        private static bool ToOracleBool(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return false;
            }

            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            text = text.Trim();
            if (string.Equals(text, "Y", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "YES", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
            {
                return number != 0;
            }

            return false;
        }


        private static void RenameParameterIfPresent(DbCommand cmd, string fromName, string toName)
        {
            foreach (DbParameter parameter in cmd.Parameters)
            {
                if (string.Equals(parameter.ParameterName, fromName, StringComparison.OrdinalIgnoreCase))
                {
                    parameter.ParameterName = toName;
                    return;
                }
            }
        }
    }
}
