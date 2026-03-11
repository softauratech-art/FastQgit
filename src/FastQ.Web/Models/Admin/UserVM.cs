
namespace FastQ.Web.Models.Admin
{
    
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

    public class UserVM
    {
        [StringLength(50)]        
        public string UserId { get; set; }

        [Display(Name = "Is Admin?")]
        public bool IsAdmin { get; set; }

        [Display(Name ="Is Active?")]
        public bool IsActive { get; set; }

        [Column("Fname")]
        public string FirstName { get; set; }

        [Column("Lname")] 
        public string LastName { get; set; }

        [StringLength(50)]
        public string Title { get; set; }

        [StringLength(100)]
        public string Email { get; set; }
        [StringLength(20)]
        public string Phone { get; set; }
        public string OtherLanguage { get; set; }

        public IDictionary<long,string>Permissions { get; set; }

    }
}