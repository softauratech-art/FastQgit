using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface ICustomerRepository
    {
        Customer Get(long id);
        Customer GetByPhone(string phone);
        void Add(Customer customer);
        void Update(Customer customer);
        IList<Customer> ListAll();
    }
}

