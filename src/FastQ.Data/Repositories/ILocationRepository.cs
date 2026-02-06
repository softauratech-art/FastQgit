using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface ILocationRepository
    {
        Location Get(long id);
        void Add(Location location);
        void Update(Location location);
        IList<Location> ListAll();
    }
}

