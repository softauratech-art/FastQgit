using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Domain.Entities;
using FastQ.Domain.Repositories;

namespace FastQ.Infrastructure.InMemory
{
    public class InMemoryProviderRepository : IProviderRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryProviderRepository(InMemoryStore store = null)
        {
            _store = store ?? InMemoryStore.Instance;
            _store.EnsureSeeded();
        }

        public Provider Get(Guid id)
        {
            lock (_store.Sync)
                return _store.Providers.TryGetValue(id, out var p) ? p : null;
        }

        public void Add(Provider provider)
        {
            lock (_store.Sync)
                _store.Providers[provider.Id] = provider;
        }

        public IList<Provider> ListByLocation(Guid locationId)
        {
            lock (_store.Sync)
                return _store.Providers.Values.Where(p => p.LocationId == locationId).ToList();
        }

        public IList<Provider> ListAll()
        {
            lock (_store.Sync)
                return _store.Providers.Values.ToList();
        }
    }
}
