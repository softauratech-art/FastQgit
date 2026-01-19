using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Data.Common;

namespace FastQ.Data.InMemory
{
    public class InMemoryCustomerRepository : ICustomerRepository
    {
        private readonly InMemoryStore _store;

        public InMemoryCustomerRepository(InMemoryStore store = null)
        {
            _store = store ?? InMemoryStore.Instance;
            _store.EnsureSeeded();
        }

        public Customer Get(Guid id)
        {
            lock (_store.Sync)
                return _store.Customers.TryGetValue(id, out var c) ? c : null;
        }

        public Customer GetByPhone(string phone)
        {
            phone = phone?.Trim();
            if (string.IsNullOrEmpty(phone)) return null;

            lock (_store.Sync)
                return _store.Customers.Values.FirstOrDefault(c => c.Phone == phone);
        }

        public void Add(Customer customer)
        {
            lock (_store.Sync)
            {
                if (!IdMapper.TryToLong(customer.Id, out _))
                {
                    customer.Id = IdMapper.FromLong(_store.NextCustomerId());
                }
                _store.Customers[customer.Id] = customer;
            }
        }

        public void Update(Customer customer)
        {
            lock (_store.Sync)
                _store.Customers[customer.Id] = customer;
        }

        public IList<Customer> ListAll()
        {
            lock (_store.Sync)
                return _store.Customers.Values.ToList();
        }
    }
}

