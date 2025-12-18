using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Domain.Entities;
using FastQ.Domain.Repositories;

namespace FastQ.Infrastructure.InMemory
{
    public class InMemoryQueueRepository : IQueueRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryQueueRepository(InMemoryStore store = null)
        {
            _store = store ?? InMemoryStore.Instance;
            _store.EnsureSeeded();
        }

        public Queue Get(Guid id)
        {
            lock (_store.Sync)
                return _store.Queues.TryGetValue(id, out var q) ? q : null;
        }

        public void Add(Queue queue)
        {
            lock (_store.Sync)
                _store.Queues[queue.Id] = queue;
        }

        public void Update(Queue queue)
        {
            lock (_store.Sync)
                _store.Queues[queue.Id] = queue;
        }

        public IList<Queue> ListByLocation(Guid locationId)
        {
            lock (_store.Sync)
                return _store.Queues.Values.Where(q => q.LocationId == locationId).ToList();
        }

        public IList<Queue> ListAll()
        {
            lock (_store.Sync)
                return _store.Queues.Values.ToList();
        }
    }
}
