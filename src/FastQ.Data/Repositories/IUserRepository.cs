using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IUserRepository
    {
        User Get(string uid, string stampuser);        
        void Add(User ouser);
        void Update(User ouser);
        IList<User> ListAll(Int32 entityid, string stampuser);
    }
}

