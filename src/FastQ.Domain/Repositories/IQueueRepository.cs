using System;
using System.Collections.Generic;
using FastQ.Domain.Entities;

namespace FastQ.Domain.Repositories
{
    public interface IQueueRepository
    {
        Queue Get(Guid id);
        void Add(Queue queue);
        void Update(Queue queue);

        IList<Queue> ListByLocation(Guid locationId);
        IList<Queue> ListAll();
    }
}
