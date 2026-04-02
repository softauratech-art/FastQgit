using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IEntityRepository
    {
        Entity Get(long id);
        void Add(Entity entity);
        void Update(Entity entity);
        IList<Entity> ListAll();
    }
}
