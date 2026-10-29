using System;
using System.Collections.Generic;
using System.Data;
namespace FastQ.Data.Db
{
    public sealed class LobbyEntry
    {
        public long QueueId { get; set; }
        public string QueueName { get; set; }
        public string SourceType { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Time { get; set; }
    }
    public sealed class DbLobbyRepository
    {
        public IList<LobbyEntry> Today(string user,long entity)
        {
            var result=new List<LobbyEntry>();
            using(var conn=DataAccess.Open())
            using(var cmd=DataAccess.CreateStoredProc(conn,"FQOWNER.FQ_GET_LOBBY"))
            {
                DataAccess.AddParam(cmd,"p_userid",user,DbType.String);
                DataAccess.AddParam(cmd,"p_entityid",entity,DbType.Int64);
                DataAccess.AddOutRefCursor(cmd,"p_cur");
                using(var r=cmd.ExecuteReader()) while(r.Read()) result.Add(new LobbyEntry {
                    QueueId=Convert.ToInt64(r["QUEUE_ID"]), QueueName=r["QUEUE_NAME"].ToString(),
                    SourceType=r["SRC_TYPE"].ToString(),Name=r["CUSTOMER_NAME"].ToString(),
                    Status=r["STATUS"].ToString(),Time=r["TIME_TEXT"].ToString() });
            }
            return result;
        }
    }
}
