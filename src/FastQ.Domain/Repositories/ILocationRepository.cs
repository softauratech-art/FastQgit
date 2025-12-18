using System;
using System.Collections.Generic;
using FastQ.Domain.Entities;

namespace FastQ.Domain.Repositories
{
    public interface ILocationRepository
    {
        Location Get(Guid id);
        void Add(Location location);
        void Update(Location location);
        IList<Location> ListAll();
    }
}
