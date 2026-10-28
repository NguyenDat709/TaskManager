using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class AuthViewModel
    {
        public string ActiveTab { get; set; } = "login";

        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}