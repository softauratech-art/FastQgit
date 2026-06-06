using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IUserRepository
    {
        User Get(string uid, string stampuser);
        void AddOrUpdateUser(string action, User ouser, long entityid, string hostqueues, string providerqueues, string reporterqueues, string queueadminqueues, string stampuser);
 		IList<User> ListAll(long entityid, string stampuser, bool loadfullprofile);
        public void Delete(string uid, long entityid, string stampuser);
        IList<UserQueuePermission> GetActionQueuePermissions(string uid);
    }
}

