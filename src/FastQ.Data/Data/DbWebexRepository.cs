
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using System;
using System.Data;

namespace FastQ.Data.Db
{
    public sealed class DbWebexRepository : IWebexRepository
    {
        public DbWebexRepository()
        {
        }
        public void LogWebexRequestToDB(string srctype, long srcid, string apiurl, string requestjson, int statuscode, string responsejson, string stampuser)
        {
            try
            {
                using var conn = DataAccess.Open();
                string sp_name = "fqowner.FQ_WEBEX_SERVICE.LOG_WEBEX_REQUEST";

                using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
                {
                    DataAccess.AddParam(cmd, "p_srctype", srctype, DbType.String);
                    DataAccess.AddParam(cmd, "p_srcid", srcid, DbType.Int64);
                    DataAccess.AddParam(cmd, "p_request_url", apiurl, DbType.String);
                    DataAccess.AddParam(cmd, "p_request_data", requestjson, DbType.String);
                    DataAccess.AddParam(cmd, "p_response_code", statuscode, DbType.Int32);
                    DataAccess.AddParam(cmd, "p_response_data", responsejson, DbType.String);
                    DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                    DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                    cmd.Parameters["p_outmsg"].Size = 4000;
                    cmd.ExecuteNonQuery();
                    //Ignore error
                    //var dberr = cmd.Parameters["p_outmsg"].Value as string;
                    //if (dberr != null) throw new InvalidOperationException("DB Error: " + dberr);
                }
            } 
            catch (Exception ex)
            {
                //Ignore error
                return;
            }
        }

        public WebexFastQRecord GetSourceDetails(string srctype, long srcid)
        {
            try
            {
                string sp_name = "fqowner.FQ_WEBEX_SERVICE.GET_SRC_RECORD_DETAILS";
                using var conn = DataAccess.Open();
                using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
                {
                    DataAccess.AddParam(cmd, "p_srctype", srctype, DbType.String);
                    DataAccess.AddParam(cmd, "p_srcid", srcid, DbType.Int64);
                    //DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                    //cmd.Parameters["p_outmsg"].Size = 4000;             
                    DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        WebexFastQRecord srcdata = new WebexFastQRecord {
                            SrcId = Convert.ToInt64(reader["SRC_ID"].ToString()),
                            SrcType = srctype,
                            WebexMeetingId = reader["MEETINGURL_HOST"].ToString(),
                            CustomerName = reader["CUSTOMER_NAME"].ToString(),
                            EmailAddress = reader["CUSTOMER_EMAIL"].ToString()
                        };
                                                
                        return srcdata;
                        
                    }
                }
            }
            catch (Exception ex)
            {
                //Ignore error
                return null;
            }
        }
    
    }
}
