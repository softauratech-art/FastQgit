using System.ComponentModel.DataAnnotations;

namespace FT.FastQ.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter valid Email address")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        public bool ShowCodeVerification { get; set; }
        public string CodeMessage { get; set; }
        public string EmailError { get; set; }
        public string CodeError { get; set; }
    }
}


