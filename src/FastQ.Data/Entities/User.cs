using System;
using System.Collections;
using System.Collections.Generic;

namespace FastQ.Data.Entities
{
    public class User
    {
        public string UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; } // required
        public bool ActiveFlag { get; set; } = true;
        public string Language { get; set; }
        public string Title { get; set; }

        public string StampUser { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime StampDateUtc { get; set; }

        public IList<UserQueuePermission> Queues { get; set; }

        public IList<UserEntity> BusinessEntities { get; set; }
        //TODO: Add other fields

    }
    public class UserEntity
    {
        public int EntityId { get; set; }
        public bool ConfigAdminFlag { get; set; } = false;

        public bool ActiveFlag { get; set; } = true;
    }
    public class UserQueuePermission
    {
        public string UserId { get; set; }
        public long QueueId { get; set; }

        public int EntityId { get; set; }
        public string QueueName { get; set; }
        public bool HostFlag { get; set; }
        public bool ProviderFlag { get; set; }
        public bool ReporterFlag { get; set; }
        public bool QueueAdminFlag { get; set; }

        public bool QueueActiveFlag { get; set; }

        //TODO: Add other fields

    }
}

