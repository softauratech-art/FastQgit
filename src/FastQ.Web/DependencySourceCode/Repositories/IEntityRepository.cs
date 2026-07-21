using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IEntityRepository
    {
        Entity Get(long id);
        void Update(Entity entity, string stampuser);

        // Add and ListAll - only available to ISS-LDMS team members
        IList<Entity> ListAll();
        //void Add(Entity entity, string stampuser);
    }
}
