using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using FastQ.Domain.Entities;
using FastQ.Domain.Repositories;
using FastQ.Infrastructure.Common;

namespace FastQ.Infrastructure.Oracle
{
    public sealed class OracleAppointmentRepository : IAppointmentRepository
    {
        private readonly string _connectionString;

        public OracleAppointmentRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Appointment Get(Guid id)
        {
            if (!IdMapper.TryToLong(id, out var apptId)) return null;

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"SELECT a.APPOINTMENT_ID, a.CUSTOMER_ID, a.QUEUE_ID, a.APPT_DATE, a.STATUS, a.CREATEDON, a.STAMPDATE,
                         q.LOCATION_ID
                  FROM APPOINTMENTS a
                  JOIN VALIDQUEUES q ON q.QUEUE_ID = a.QUEUE_ID
                  WHERE a.APPOINTMENT_ID = :apptId"))
            {
                OracleDb.AddParam(cmd, "apptId", apptId, DbType.Int64);
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? MapAppointment(reader) : null;
                }
            }
        }

        public void Add(Appointment appointment)
        {
            using (var conn = OracleDb.Open(_connectionString))
            {
                var newId = OracleDb.NextVal(conn, "APPTSEQ");
                appointment.Id = IdMapper.FromLong(newId);

                if (!IdMapper.TryToLong(appointment.CustomerId, out var customerId))
                    throw new InvalidOperationException("CustomerId is not mapped to a numeric ID.");
                if (!IdMapper.TryToLong(appointment.QueueId, out var queueId))
                    throw new InvalidOperationException("QueueId is not mapped to a numeric ID.");

                using (var cmd = OracleDb.CreateCommand(conn,
                    @"INSERT INTO APPOINTMENTS
                        (APPOINTMENT_ID, CUSTOMER_ID, QUEUE_ID, STATUS, APPT_DATE, CREATEDBY, CREATEDON, STAMPUSER, STAMPDATE)
                      VALUES
                        (:apptId, :customerId, :queueId, :status, :apptDate, :createdBy, SYSDATE, :stampUser, SYSDATE)"))
                {
                    OracleDb.AddParam(cmd, "apptId", newId, DbType.Int64);
                    OracleDb.AddParam(cmd, "customerId", customerId, DbType.Int64);
                    OracleDb.AddParam(cmd, "queueId", queueId, DbType.Int64);
                    OracleDb.AddParam(cmd, "status", appointment.Status.ToString(), DbType.String);
                    OracleDb.AddParam(cmd, "apptDate", appointment.ScheduledForUtc, DbType.DateTime);
                    OracleDb.AddParam(cmd, "createdBy", "fastq", DbType.String);
                    OracleDb.AddParam(cmd, "stampUser", "fastq", DbType.String);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Appointment appointment)
        {
            if (!IdMapper.TryToLong(appointment.Id, out var apptId))
                throw new InvalidOperationException("Appointment Id is not mapped to a numeric ID.");
            if (!IdMapper.TryToLong(appointment.CustomerId, out var customerId))
                throw new InvalidOperationException("CustomerId is not mapped to a numeric ID.");
            if (!IdMapper.TryToLong(appointment.QueueId, out var queueId))
                throw new InvalidOperationException("QueueId is not mapped to a numeric ID.");

            using (var conn = OracleDb.Open(_connectionString))
            using (var cmd = OracleDb.CreateCommand(conn,
                @"UPDATE APPOINTMENTS
                  SET CUSTOMER_ID = :customerId,
                      QUEUE_ID = :queueId,
                      STATUS = :status,
                      APPT_DATE = :apptDate,
                      STAMPUSER = :stampUser,
                      STAMPDATE = SYSDATE
                  WHERE APPOINTMENT_ID = :apptId"))
            {
                OracleDb.AddParam(cmd, "customerId", customerId, DbType.Int64);
                OracleDb.AddParam(cmd, "queueId", queueId, DbType.Int64);
                OracleDb.AddParam(cmd, "status", appointment.Status.ToString(), DbType.String);
                OracleDb.AddParam(cmd, "apptDate", appointment.ScheduledForUtc, DbType.DateTime);
                OracleDb.AddParam(cmd, "stampUser", "fastq", DbType.String);
                OracleDb.AddParam(cmd, "apptId", apptId, DbType.Int64);
                cmd.ExecuteNonQuery();
            }
        }

        public IList<Appointment> ListByQueue(Guid queueId)
        {
            if (!IdMapper.TryToLong(queueId, out var qid)) return new List<Appointment>();
            return ListByFilter("a.QUEUE_ID = :queueId", cmd => OracleDb.AddParam(cmd, "queueId", qid, DbType.Int64));
        }

        public IList<Appointment> ListByCustomer(Guid customerId)
        {
            if (!IdMapper.TryToLong(customerId, out var cid)) return new List<Appointment>();
            return ListByFilter("a.CUSTOMER_ID = :customerId", cmd => OracleDb.AddParam(cmd, "customerId", cid, DbType.Int64));
        }

        public IList<Appointment> ListByLocation(Guid locationId)
        {
            if (!IdMapper.TryToLong(locationId, out var lid)) return new List<Appointment>();
            return ListByFilter("q.LOCATION_ID = :locationId", cmd => OracleDb.AddParam(cmd, "locationId", lid, DbType.Int64));
        }

        public IList<Appointment> ListAll()
        {
            return ListByFilter(null, null);
        }

        private IList<Appointment> ListByFilter(string whereClause, Action<DbCommand> addParams)
        {
            var list = new List<Appointment>();
            var sql = @"SELECT a.APPOINTMENT_ID, a.CUSTOMER_ID, a.QUEUE_ID, a.APPT_DATE, a.STATUS, a.CREATEDON, a.STAMPDATE,
                               q.LOCATION_ID
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
                        list.Add(MapAppointment(reader));
                    }
                }
            }

            return list;
        }

        private static Appointment MapAppointment(IDataRecord record)
        {
            var apptId = Convert.ToInt64(record["APPOINTMENT_ID"]);
            var customerId = Convert.ToInt64(record["CUSTOMER_ID"]);
            var queueId = Convert.ToInt64(record["QUEUE_ID"]);
            var locationId = Convert.ToInt64(record["LOCATION_ID"]);
            var apptDate = record["APPT_DATE"] == DBNull.Value ? DateTime.UtcNow : Convert.ToDateTime(record["APPT_DATE"]);
            var createdOn = record["CREATEDON"] == DBNull.Value ? apptDate : Convert.ToDateTime(record["CREATEDON"]);
            var stampDate = record["STAMPDATE"] == DBNull.Value ? createdOn : Convert.ToDateTime(record["STAMPDATE"]);
            var statusText = record["STATUS"] == DBNull.Value ? AppointmentStatus.Scheduled.ToString() : record["STATUS"].ToString();

            Enum.TryParse(statusText, out AppointmentStatus status);

            return new Appointment
            {
                Id = IdMapper.FromLong(apptId),
                CustomerId = IdMapper.FromLong(customerId),
                QueueId = IdMapper.FromLong(queueId),
                LocationId = IdMapper.FromLong(locationId),
                ScheduledForUtc = DateTime.SpecifyKind(apptDate, DateTimeKind.Utc),
                Status = status,
                CreatedUtc = DateTime.SpecifyKind(createdOn, DateTimeKind.Utc),
                UpdatedUtc = DateTime.SpecifyKind(stampDate, DateTimeKind.Utc)
            };
        }
    }
}
