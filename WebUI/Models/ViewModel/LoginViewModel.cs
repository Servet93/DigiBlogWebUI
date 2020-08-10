using System.ComponentModel.DataAnnotations;

namespace Blog.WebUI.Models
{
    public class LoginViewModel
    {
        [Display(Name = "Email")]
        [Required(ErrorMessage = "{0} Gerekli!")]
        public string EmailOrUserName { get; set; }

        [Display(Name = "Þifre")]
        [Required(ErrorMessage = "{0} Gerekli!")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Beni Hatýrla?")]
        public bool RememberMe { get; set; }
    }
}
