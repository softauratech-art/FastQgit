using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IWebexRepository
    {
        public void LogWebexRequestToDB(string srctype, long srcid, string apiurl, string requestjson, int statuscode, string responsejson, string stampuser);
        public WebexFastQRecord GetSourceDetails(string srcType, long srcId);
    }
}
