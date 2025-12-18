using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Domain.Entities;
using FastQ.Domain.Repositories;

namespace FastQ.Infrastructure.InMemory
{
    public class InMemoryLocationRepository : ILocationRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryLocationRepository(InMemoryStore store = null)
        {
            _store = store ?? InMemoryStore.Instance;
            _store.EnsureSeeded();
        }

        public Location Get(Guid id)
        {
            lock (_store.Sync)
                return _store.Locations.TryGetValue(id, out var l) ? l : null;
        }

        public void Add(Location location)
        {
            lock (_store.Sync)
                _store.Locations[location.Id] = location;
        }

        public void Update(Location location)
        {
            lock (_store.Sync)
                _store.Locations[location.Id] = location;
        }

        public IList<Location> ListAll()
        {
            lock (_store.Sync)
                return _store.Locations.Values.ToList();
        }
    }
}
