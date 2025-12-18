using System;
using System.Collections.Generic;
using FastQ.Domain.Entities;

namespace FastQ.Domain.Repositories
{
    public interface ICustomerRepository
    {
        Customer Get(Guid id);
        Customer GetByPhone(string phone);
        void Add(Customer customer);
        void Update(Customer customer);
        IList<Customer> ListAll();
    }
}
