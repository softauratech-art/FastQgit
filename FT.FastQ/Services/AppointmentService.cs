using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using Oracle.ManagedDataAccess.Client;
using FT.FastQ.Models;
using FT.FastQ.DataAccess;
using NLog;

namespace FT.FastQ.Services
{
    public class AppointmentService
    {
        private static Logger _logger = LogManager.GetLogger("AppointmentService");

        public AppointmentService()
        {
        }

        public List<Appointment> GetAppointmentsByEmail(string email)
        {
            List<Appointment> appointments = new List<Appointment>();

            try
            {
                string storedProcedure = "SP_GET_APPOINTMENTS_BY_EMAIL";

                OracleParameter[] parameters = new OracleParameter[]
                {
                    new OracleParameter("P_EMAIL", OracleDbType.Varchar2) { Value = email }
                };

                DataTable dt = DataAccess.DataAccess.FillTable(storedProcedure, parameters, DataAccess.DataAccess.DatabaseSchema.FR);

                foreach (DataRow row in dt.Rows)
                {
                    appointments.Add(new Appointment
                    {
                        Id = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0,
                        Email = row["EMAIL"] != DBNull.Value ? row["EMAIL"].ToString() : "",
                        AppointmentDateTime = row["APPOINTMENT_DATETIME"] != DBNull.Value ? Convert.ToDateTime(row["APPOINTMENT_DATETIME"]) : DateTime.MinValue,
                        Queue = row["QUEUE"] != DBNull.Value ? row["QUEUE"].ToString() : "",
                        Type = row["TYPE"] != DBNull.Value ? row["TYPE"].ToString() : "",
                        Status = row["STATUS"] != DBNull.Value ? row["STATUS"].ToString() : "",
                        CreatedAt = row["CREATED_AT"] != DBNull.Value ? Convert.ToDateTime(row["CREATED_AT"]) : DateTime.MinValue
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error fetching appointments: {ex.Message} - {ex.StackTrace}");
                // Return empty list on error
            }

            return appointments;
        }

        public Appointment GetAppointmentById(int appointmentId, string email)
        {
            try
            {
                string storedProcedure = "SP_GET_APPOINTMENT_BY_ID";

                OracleParameter[] parameters = new OracleParameter[]
                {
                    new OracleParameter("P_APPOINTMENT_ID", OracleDbType.Int32) { Value = appointmentId },
                    new OracleParameter("P_EMAIL", OracleDbType.Varchar2) { Value = email }
                };

                DataTable dt = DataAccess.DataAccess.FillTable(storedProcedure, parameters, DataAccess.DataAccess.DatabaseSchema.FR);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    var appointment = new Appointment
                    {
                        Id = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0,
                        Email = row["EMAIL"] != DBNull.Value ? row["EMAIL"].ToString() : "",
                        AppointmentDateTime = row["APPOINTMENT_DATETIME"] != DBNull.Value ? Convert.ToDateTime(row["APPOINTMENT_DATETIME"]) : DateTime.MinValue,
                        Queue = row["QUEUE"] != DBNull.Value ? row["QUEUE"].ToString() : "",
                        Type = row["TYPE"] != DBNull.Value ? row["TYPE"].ToString() : "",
                        Status = row["STATUS"] != DBNull.Value ? row["STATUS"].ToString() : "",
                        CreatedAt = row["CREATED_AT"] != DBNull.Value ? Convert.ToDateTime(row["CREATED_AT"]) : DateTime.MinValue
                    };
                    
                    return appointment;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error fetching appointment: {ex.Message} - {ex.StackTrace}");
            }

            return null;
        }

        public string GetCancelReason(int appointmentId, string email)
        {
            try
            {
                string storedProcedure = "SP_GET_CANCEL_REASON";

                OracleParameter[] parameters = new OracleParameter[]
                {
                    new OracleParameter("P_APPOINTMENT_ID", OracleDbType.Int32) { Value = appointmentId },
                    new OracleParameter("P_EMAIL", OracleDbType.Varchar2) { Value = email }
                };

                DataTable dt = DataAccess.DataAccess.FillTable(storedProcedure, parameters, DataAccess.DataAccess.DatabaseSchema.FR);

                if (dt.Rows.Count > 0 && dt.Rows[0]["CANCEL_REASON"] != DBNull.Value)
                {
                    return dt.Rows[0]["CANCEL_REASON"].ToString();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting cancel reason: {ex.Message} - {ex.StackTrace}");
            }
            
            return "";
        }

        public bool CancelAppointment(int appointmentId, string email, string reason = null)
        {
            try
            {
                string storedProcedure = "SP_CANCEL_APPOINTMENT";

                OracleParameter[] parameters = new OracleParameter[]
                {
                    new OracleParameter("P_APPOINTMENT_ID", OracleDbType.Int32) { Value = appointmentId },
                    new OracleParameter("P_EMAIL", OracleDbType.Varchar2) { Value = email },
                    new OracleParameter("P_REASON", OracleDbType.Varchar2) { Value = (object)reason ?? DBNull.Value }
                };

                int rowsAffected = DataAccess.DataAccess.ExecuteNonQuery(storedProcedure, ref parameters, DataAccess.DataAccess.DatabaseSchema.FR);
                
                if (rowsAffected > 0)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error cancelling appointment: {ex.Message} - {ex.StackTrace}");
                return false;
            }
            
            return false;
        }
    }
}
