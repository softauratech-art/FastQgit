using NLog;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;

namespace FT.FastQ.DataAccess
{
    public static class DataAccess
    {

        static Logger sqllogger = LogManager.GetLogger("SQL.FT.FastQ.DataAccess");

        //private static string sParmNameAndVal = "";
        public enum DatabaseSchema
        {
            FR
        }

        public enum OraCmdType
        {
            StoredProcedure,
            SQLText
        }

        public static string GetDBConnectionString(DatabaseSchema aDBSchema)
        {
            return GetConnectionString(aDBSchema);
        }

        private static string GetConnectionString(DatabaseSchema aDBSchema)
        {

            string Env = ConfigurationManager.AppSettings["Environment"].ToString();      //-- get Environment key from machine.config
            string sConnection = string.Empty;

            if (aDBSchema == DatabaseSchema.FR)
                sConnection = ConfigurationManager.ConnectionStrings[string.Format("Conn_FR_{0}", Env)].ToString();

            return sConnection;
        }

        public static DataTable FillTable(string aQueryTextCommandName, OraCmdType aOraCmdType, DatabaseSchema aDBSchema)
        {
            DataTable dt = new DataTable();
            using (OracleConnection oConn = new OracleConnection(GetConnectionString(aDBSchema)))
            {
                using (OracleCommand oCmd = new OracleCommand(aQueryTextCommandName, oConn))
                {
                    sqllogger.Info(aQueryTextCommandName.ToString());
                    oConn.Open();
                    using (OracleDataAdapter da = new OracleDataAdapter(oCmd))
                    {
                        oCmd.InitialLONGFetchSize = 2000;

                        try
                        {
                            da.Fill(dt);
                        }
                        catch (Exception ex)
                        {
                            sqllogger.Error(ex.ToString());
                            //string notificationLocation = string.Format("{0}.{1}()", MethodBase.GetCurrentMethod().ReflectedType.Name, MethodBase.GetCurrentMethod().Name);
                            throw (ex);
                        }
                    }
                }
            }
            return dt;
        }

        public static DataTable FillTable(string aOraCmdName, OracleParameter[] aParameters, DatabaseSchema aDBSchema)
        {
            string sParmNameAndVal = "";
            //string userremoteip = !System.Web.HttpContext.Current.IsNullOrEmpty() ? ActionValidator.GetIPAddress() + "," : string.Empty;
            System.Data.DataTable dt = new System.Data.DataTable();
            try
            {

                using (OracleConnection oConn = new OracleConnection(GetConnectionString(aDBSchema)))
                {
                    using (OracleCommand oCmd = new OracleCommand(aOraCmdName, oConn))
                    {

                        sqllogger.Info(aOraCmdName.ToString());
                        oConn.Open();
                        oCmd.CommandType = CommandType.StoredProcedure;
                        oCmd.InitialLONGFetchSize = 2000;
                        foreach (OracleParameter oparam in aParameters)
                        {
                            sParmNameAndVal = ((oparam.ParameterName == null) ? "" : oparam.ParameterName) + "=" + ((oparam.Value == null) ? "" : oparam.Value);
                            //sqllogger.Info(sParmNameAndVal);
                            oCmd.Parameters.Add(oparam);
                        }
                        //sqllogger.Info(oCmd.ToString());
                        oCmd.Prepare();
                        using (OracleDataAdapter da = new OracleDataAdapter(oCmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            }
            catch (OracleException ex) { sqllogger.Error(ex.Message + ex.StackTrace, ex); }
            catch (Exception e)
            {
                var exc = e;
                sqllogger.Error(exc.ToString());
            }

            sqllogger.Info("DataTable Returned");
            return dt;
        }


        public static int ExecuteNonQuery(string sqlCommand, DatabaseSchema aDBSchema)
        {
            int retval = 0;

            using (OracleConnection oConn = new OracleConnection(GetConnectionString(aDBSchema)))
            {
                using (OracleCommand oCmd = new OracleCommand(sqlCommand, oConn))
                {
                    sqllogger.Info(sqlCommand.ToString());
                    oCmd.CommandType = CommandType.Text;

                    try
                    {
                        oConn.Open();
                        retval = oCmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        sqllogger.Error(ex);
                        retval = -999;
                    }
                }
            }

            return retval;
        }

        public static int ExecuteNonQuery(string aOraCmdName, ref OracleParameter[] aParameters, DatabaseSchema aDBSchema)
        {
            string sParmNameAndVal = "";
            int retval = 0;

            using (OracleConnection oConn = new OracleConnection(GetConnectionString(aDBSchema)))
            {
                using (OracleCommand oCmd = new OracleCommand(aOraCmdName, oConn))
                {
                    oConn.Open();
                    oCmd.CommandType = CommandType.StoredProcedure;

                    sqllogger.Info(aOraCmdName.ToString());
                    foreach (OracleParameter oparam in aParameters)
                    {
                        sParmNameAndVal = ((oparam.ParameterName == null) ? "" : oparam.ParameterName) + "=" + ((oparam.Value == null) ? "" : oparam.Value);
                        sqllogger.Info(sParmNameAndVal);
                        oCmd.Parameters.Add(oparam);
                    }

                    retval = oCmd.ExecuteNonQuery();
                }
            }

            return retval;

        }

        public static int ExecuteNonQuery(string aOraCmdName, ref OracleParameter[] aParameters, int arraybindcount, DatabaseSchema aDBSchema)
        {
            string sParmNameAndVal = "";
            int retval = 0;
            //Try

            using (OracleConnection oConn = new OracleConnection(GetConnectionString(aDBSchema)))
            {
                using (OracleCommand oCmd = new OracleCommand(aOraCmdName, oConn))
                {
                    oConn.Open();
                    oCmd.CommandType = CommandType.StoredProcedure;
                    // Set the ArrayBindCount to indicate the number of values 
                    oCmd.ArrayBindCount = arraybindcount;
                    sqllogger.Info(aOraCmdName.ToString());
                    foreach (OracleParameter oparam in aParameters)
                    {
                        sParmNameAndVal = ((oparam.ParameterName == null) ? "" : oparam.ParameterName) + "=" + ((oparam.Value == null) ? "" : oparam.Value);
                        sqllogger.Info(sParmNameAndVal);
                        oCmd.Parameters.Add(oparam);
                    }

                    retval = oCmd.ExecuteNonQuery();
                }
            }

            return retval;

        }

        public static ExecuteScalerReturnValue ExecuteScaler(string sqlCommand, DatabaseSchema aDBSchema)
        {
            ExecuteScalerReturnValue retval = new ExecuteScalerReturnValue();

            using (OracleConnection oConn = new OracleConnection(GetConnectionString(aDBSchema)))
            {
                using (OracleCommand oCmd = new OracleCommand(sqlCommand, oConn))
                {
                    sqllogger.Info(sqlCommand.ToString());
                    oCmd.CommandType = CommandType.Text;

                    try
                    {
                        oConn.Open();
                        retval.Value = oCmd.ExecuteScalar();
                        retval.IsSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        retval.IsSuccessful = false;
                        retval.Message = ex.ToString();
                    }
                }
            }

            return retval;
        }

        // ========== ASYNC METHODS FOR .NET 4.8 WITH ASYNC/AWAIT SUPPORT ==========

        public static async Task<DataTable> FillTableAsync(string aOraCmdName, OracleParameter[] aParameters, DatabaseSchema aDBSchema)
        {
            // Use the synchronous FillTable method which is known to work correctly with RefCursor parameters
            // Wrap it in Task.Run to make it async-compatible
            // This avoids the ORA-24338 error that occurs with OracleDataAdapter in async contexts
            return await Task.Run(() => FillTable(aOraCmdName, aParameters, aDBSchema)).ConfigureAwait(false);
        }

        public static async Task<int> ExecuteNonQueryAsync(string sqlCommand, DatabaseSchema aDBSchema)
        {
            int retval = 0;

            using (OracleConnection oConn = new OracleConnection(GetConnectionString(aDBSchema)))
            {
                using (OracleCommand oCmd = new OracleCommand(sqlCommand, oConn))
                {
                    sqllogger.Info(sqlCommand.ToString());
                    oCmd.CommandType = CommandType.Text;

                    try
                    {
                        await oConn.OpenAsync().ConfigureAwait(false);
                        retval = await oCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        sqllogger.Error(ex);
                        retval = -999;
                    }
                }
            }

            return retval;
        }

        public static async Task<int> ExecuteNonQueryAsync(string aOraCmdName, OracleParameter[] aParameters, DatabaseSchema aDBSchema)
        {
            string sParmNameAndVal = "";
            int retval = 0;

            using (OracleConnection oConn = new OracleConnection(GetConnectionString(aDBSchema)))
            {
                await oConn.OpenAsync().ConfigureAwait(false);
                using (OracleCommand oCmd = new OracleCommand(aOraCmdName, oConn))
                {
                    oCmd.CommandType = CommandType.StoredProcedure;

                    sqllogger.Info(aOraCmdName.ToString());
                    foreach (OracleParameter oparam in aParameters)
                    {
                        sParmNameAndVal = ((oparam.ParameterName == null) ? "" : oparam.ParameterName) + "=" + ((oparam.Value == null) ? "" : oparam.Value);
                        sqllogger.Info(sParmNameAndVal);
                        oCmd.Parameters.Add(oparam);
                    }

                    retval = await oCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }

            return retval;
        }

        public static async Task<ExecuteScalerReturnValue> ExecuteScalerAsync(string sqlCommand, DatabaseSchema aDBSchema)
        {
            ExecuteScalerReturnValue retval = new ExecuteScalerReturnValue();

            using (OracleConnection oConn = new OracleConnection(GetConnectionString(aDBSchema)))
            {
                using (OracleCommand oCmd = new OracleCommand(sqlCommand, oConn))
                {
                    sqllogger.Info(sqlCommand.ToString());
                    oCmd.CommandType = CommandType.Text;

                    try
                    {
                        await oConn.OpenAsync().ConfigureAwait(false);
                        retval.Value = await oCmd.ExecuteScalarAsync().ConfigureAwait(false);
                        retval.IsSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        retval.IsSuccessful = false;
                        retval.Message = ex.ToString();
                    }
                }
            }

            return retval;
        }

    }

    public class ExecuteScalerReturnValue
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; }
        public object Value { get; set; }

        public ExecuteScalerReturnValue()
        {
            IsSuccessful = false;
            Message = string.Empty;
            Value = null;
        }
    }

}







