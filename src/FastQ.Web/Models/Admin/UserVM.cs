
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

        public bool IsAdmin { get; set; }

        public bool IsActive { get; set; }

        [Column("Fname")]
        public string FirstName { get; set; }

        [Column("Lname")] 
        public string LastName { get; set; }

        [StringLength(50)]
        public string Title { get; set; }

        [StringLength(100)]
        public string Email { get; set; }
        public string OtherLanguage { get; set; }

        public IList<string>Permissions { get; set; }


    }
}