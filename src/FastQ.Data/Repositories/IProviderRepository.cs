using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IProviderRepository
    {               
        IList<Provider> ListByEntity(long entityId);       
    }
}

