using System;
using System.Configuration;
using Oracle.ManagedDataAccess.Client;
using FT.FastQ.Models;
using System.Net.Mail;
using FT.FastQ.DataAccess;
using NLog;
using System.Data;

namespace FT.FastQ.Services
{
    public class AuthenticationService
    {
        private static Logger _logger = LogManager.GetLogger("AuthenticationService");

        public AuthenticationService()
        {
        }

        public bool EmailExistsInDatabase(string email)
        {
            try
            {
                string storedProcedure = "SP_CHECK_EMAIL_EXISTS";

                OracleParameter[] parameters = new OracleParameter[]
                {
                    new OracleParameter("P_EMAIL", OracleDbType.Varchar2) { Value = email }
                };

                DataTable dt = DataAccess.DataAccess.FillTable(storedProcedure, parameters, DataAccess.DataAccess.DatabaseSchema.FR);

                if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                {
                    decimal count = Convert.ToDecimal(dt.Rows[0][0]);
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking email: {ex.Message} - {ex.StackTrace}");
                return true; // For development
            }
            
            return false;
        }

        public string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        ////public void StoreVerificationCode(string email, string code)
        ////{
        ////    try
        ////    {
        ////        int expirationMinutes = int.Parse(ConfigurationManager.AppSettings["VerificationCode:ExpirationMinutes"] ?? "10");
        ////        DateTime expirationTime = DateTime.Now.AddMinutes(expirationMinutes);

        ////        string storedProcedure = "SP_STORE_VERIFICATION_CODE";

        ////        OracleParameter[] parameters = new OracleParameter[]
        ////        {
        ////            new OracleParameter("P_EMAIL", OracleDbType.Varchar2) { Value = email },
        ////            new OracleParameter("P_CODE", OracleDbType.Varchar2, 6) { Value = code },
        ////            new OracleParameter("P_EXPIRATION_TIME", OracleDbType.Date) { Value = expirationTime },
        ////            new OracleParameter("P_CREATED_AT", OracleDbType.Date) { Value = DateTime.Now }
        ////        };

        ////        int rowsAffected = DataAccess.DataAccess.ExecuteNonQuery(storedProcedure, ref parameters, DataAccess.DataAccess.DatabaseSchema.FR);
        ////        if (rowsAffected < 0)
        ////        {
        ////            _logger.Warn($"StoreVerificationCode returned error code: {rowsAffected}");
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        _logger.Error($"Error storing verification code: {ex.Message} - {ex.StackTrace}");
        ////    }
        ////}

        public bool VerifyCode(string email, string enteredCode)
        {
            try
            {
                string storedProcedure = "SP_VERIFY_CODE";

                OracleParameter[] parameters = new OracleParameter[]
                {
                    new OracleParameter("P_EMAIL", OracleDbType.Varchar2) { Value = email }
                };

                DataTable dt = DataAccess.DataAccess.FillTable(storedProcedure, parameters, DataAccess.DataAccess.DatabaseSchema.FR);

                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    string storedCode = row["CODE"] != DBNull.Value ? row["CODE"].ToString() : "";
                    DateTime expirationTime = row["EXPIRATIONTIME"] != DBNull.Value ? Convert.ToDateTime(row["EXPIRATIONTIME"]) : DateTime.MinValue;

                    if (DateTime.Now > expirationTime)
                    {
                        return false; // Code expired
                    }

                    return storedCode == enteredCode;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error verifying code: {ex.Message} - {ex.StackTrace}");
                return false;
            }
            
            return false;
        }

        public void SendVerificationEmail(string email, string code)
        {
            try
            {
                string fromEmail = ConfigurationManager.AppSettings["EmailService:FromEmail"] ?? "noreply@fastq.com";
                string fromName = ConfigurationManager.AppSettings["EmailService:FromName"] ?? "FastQ Authentication";
                int expirationMinutes = int.Parse(ConfigurationManager.AppSettings["VerificationCode:ExpirationMinutes"] ?? "10");

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(fromEmail, fromName);
                mail.To.Add(email);
                mail.Subject = "Your FastQ Verification Code";
                mail.Body = $"Your verification code is: {code}\n\nThis code will expire in {expirationMinutes} minutes.\n\nIf you did not request this code, please ignore this email.";
                mail.IsBodyHtml = false;

                // Use SMTP settings from web.config
                SmtpClient smtp = new SmtpClient();
                smtp.Send(mail);

                _logger.Info($"Verification email sent successfully to {email} with code: {code}");
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - allow the flow to continue
                _logger.Error($"Error sending email to {email}: {ex.Message} - {ex.StackTrace}");
                
                // For development/testing, still log the code even if email fails
                _logger.Info($"Verification code for {email}: {code}");
            }
        }
    }
}
