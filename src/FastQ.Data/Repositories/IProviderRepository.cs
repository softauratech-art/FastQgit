using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IProviderRepository
    {
        Provider Get(Guid id);
        void Add(Provider provider);
        IList<Provider> ListByLocation(Guid locationId);
        IList<Provider> ListAll();
    }
}

