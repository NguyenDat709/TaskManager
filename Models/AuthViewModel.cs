using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class AuthViewModel
    {
        // Chế độ hiển thị: "login" hoặc "register"
        public string ActiveTab { get; set; } = "login";

        // Dữ liệu Form Đăng nhập
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }

        // Dữ liệu Form Đăng ký
        public string FullName { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;

        // Thông báo lỗi hoặc thành công
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}