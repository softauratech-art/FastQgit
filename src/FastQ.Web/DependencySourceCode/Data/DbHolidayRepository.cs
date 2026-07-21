using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Data;

namespace FastQ.Data.Db
{
    public sealed class DbHolidayRepository : IHolidayRepository
    {      
        public DbHolidayRepository()
        {    
        }

        public Holiday Get(DateTime day)
        {
            string sp_name =  "fqowner.FQ_PROCS_GET.GET_HOLIDAY";
            try
            {
                using (var conn = DataAccess.Open())                                
                using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
                {
                    DataAccess.AddParam(cmd, "p_day", day.ToString(), DbType.String);
                    DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return null;

                        return new Holiday
                        {
                            Day = Convert.ToDateTime(reader["HolidayDate"].ToString()),
                            Description = reader["HolidayDesc"].ToString(),
                            ActiveFlag = (reader["ActiveFlag"]?.ToString() ?? "N") == "Y",
                            StampDate = Convert.ToDateTime(reader["StampDate"].ToString()),
                            StampUser = reader["StampUser"].ToString()
                        };
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public IList<Holiday> ListAll()
        {
            var list = new List<Holiday>();
            using (var conn = DataAccess.Open())
            {
                using (var cmd = DataAccess.CreateStoredProc(conn, "fqowner.FQ_PROCS_GET.GET_HOLIDAYS"))
                {                    
                    DataAccess.AddOutRefCursor(cmd, "p_ref_cursor");
                    using (var reader = cmd.ExecuteReader())
                    {                      
                        while (reader.Read())
                        {
                            list.Add(new Holiday
                                        {
                                            Day = Convert.ToDateTime(reader["HolidayDate"].ToString()),
                                            Description = reader["HolidayDesc"].ToString(),
                                            ActiveFlag = (reader["ActiveFlag"]?.ToString() ?? "N") == "Y",
                                            StampDate = Convert.ToDateTime(reader["StampDate"].ToString()),
                                            StampUser = reader["StampUser"].ToString()
                                        });
                        }
                    }
                }
            }

            return list;
        }

        public void AddOrUpdateHoliday(Holiday oholiday, string stampuser)
        {
            using var conn = DataAccess.Open();
            string sp_name = "FQOWNER.FQ_PROCS_ADMIN.UPSERT_HOLIDAY";
            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {
                //DataAccess.AddParam(cmd, "p_action", action, DbType.String); 
                DataAccess.AddParam(cmd, "p_day", oholiday.Day, DbType.Date);
                DataAccess.AddParam(cmd, "p_description", oholiday.Description, DbType.String);
                DataAccess.AddParam(cmd, "p_activeflag", oholiday.ActiveFlag ? "Y" : "N", DbType.String);               
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                string dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (!string.IsNullOrEmpty(dberr)) throw new InvalidOperationException("DB Error: " + dberr);
            }
        }

        public void Delete(DateTime day, string stampuser)
        {
            using var conn = DataAccess.Open();
            string sp_name = "fqowner.FQ_PROCS_ADMIN.DELETE_HOLIDAY";
            using (var cmd = DataAccess.CreateStoredProc(conn, sp_name))
            {
                DataAccess.AddParam(cmd, "p_date", day.ToString("MM/dd/yyyy"), DbType.String);
                DataAccess.AddParam(cmd, "p_stampuser", stampuser, DbType.String);
                DataAccess.AddParam(cmd, "p_outmsg", null, DbType.String).Direction = ParameterDirection.Output;
                cmd.Parameters["p_outmsg"].Size = 4000;
                cmd.ExecuteNonQuery();
                var dberr = cmd.Parameters["p_outmsg"].Value as string;
                if (dberr != null) throw new InvalidOperationException(dberr);
            }
        }
    }

}
