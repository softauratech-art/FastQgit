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
        public bool AdminFlag { get; set; }
        public bool ActiveFlag { get; set; } = true;
        public string Language { get; set; }
        public string Title { get; set; }

        public string StampUser { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime StampDateUtc { get; set; }

        public IList<UserQueues> Queues { get; set; }
        //TODO: Add other fields
       
    }

    public class UserQueues
    {
        public string UserId { get; set; }
        public string QueueId { get; set; }
        public string QueueName { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; } 
        
        //TODO: Add other fields

    }
}

