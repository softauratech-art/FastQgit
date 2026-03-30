using FastQ.Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FastQ.Web.Models.Admin
{  
    public class UserVM
    {
        [StringLength(50)]
        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; }

        [Display(Name = "Is Admin?")]
        public bool IsAdmin { get; set; }

        [Display(Name ="Is Active?")]
        public bool IsActive { get; set; }

        [Required(ErrorMessage = "FirstName is required")]        
        public string FirstName { get; set; }

        [Required(ErrorMessage = "LastName is required")]
        public string LastName { get; set; }

        [StringLength(50)]
        public string Title { get; set; }

        [StringLength(100)]
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; }
        [StringLength(20)]
        public string Phone { get; set; }
        public string OtherLanguage { get; set; }

        public IList<UserQueuePermission> Permissions { get; set; }

    }
    //public class UserQueuePermission
    //{
    //    public string UserId { get; set; }
    //    public int EntityId { get; set; }
    //    public string QueueName { get; set; }
    //
    //    public long QueueId { get; set; }
    //    public bool HostFlag { get; set; }
    //    public bool ProviderFlag { get; set; }
    //    public bool ReporterFlag { get; set; }
    //    public bool QueueAdminFlag { get; set; }
    //    public bool QueueActiveFlag { get; set; }
    //}
    //public class QueuePermission
    //{
    //    public long QueueId { get; set; }
    //    public bool IsHost { get; set; }
    //    public bool IsProvider { get; set; }
    //    public  bool IsReporter { get; set; }
    //    public bool IsQueueAdmin { get; set; }

    //}
}