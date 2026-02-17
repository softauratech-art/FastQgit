using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IQueueRepository
    {
        Queue Get(long id);
        void Add(Queue queue);
        void Update(Queue queue);

        IList<Queue> ListByLocation(long locationId);
        IList<Queue> ListAll();
        IList<Tuple<long, string>> ListServicesByQueue(long queueId);
    }
}

