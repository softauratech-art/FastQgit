using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IProviderRepository
    {
        Provider Get(string id);
        void Add(Provider provider);
        IList<Provider> ListByLocation(long locationId);
        IList<Provider> ListAll();
    }
}

