using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _db;

        public AuthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string mode = "login", string? registered = null)
        {
            var vm = new AuthViewModel
            {
                ActiveTab = mode == "register" ? "register" : "login",
                SuccessMessage = registered == "true" ? "Đăng ký tài khoản thành công! Vui lòng đăng nhập." : null
            };
            return View(vm);
        }

        [HttpPost]
        public IActionResult Login(AuthViewModel model)
        {
            model.ActiveTab = "login";

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                model.ErrorMessage = "Vui lòng nhập đầy đủ Email công việc và Mật khẩu!";
                return View(model);
            }

            try
            {
                string emailInput = model.Email.Trim().ToLower();
                var user = _db.Users.FirstOrDefault(u => u.Email.ToLower() == emailInput);

                if (user == null)
                {
                    model.ErrorMessage = "Email này chưa tồn tại trong hệ thống!";
                    return View(model);
                }

                if (!string.IsNullOrEmpty(user.PasswordHash) && user.PasswordHash != model.Password)
                {
                    model.ErrorMessage = "Mật khẩu không chính xác!";
                    return View(model);
                }

                var cookieOptions = new CookieOptions
                {
                    Expires = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8),
                    HttpOnly = true
                };

                Response.Cookies.Append("TaskWeb_UserId", user.UserID.ToString(), cookieOptions);
                Response.Cookies.Append("TaskWeb_UserName", user.FullName, cookieOptions);
                Response.Cookies.Append("TaskWeb_UserEmail", user.Email, cookieOptions);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                model.ErrorMessage = "Lỗi kết nối CSDL MySQL: " + ex.Message;
                return View(model);
            }
        }

        [HttpPost]
        public IActionResult Register(AuthViewModel model)
        {
            model.ActiveTab = "register";

            if (string.IsNullOrWhiteSpace(model.FullName) ||
                string.IsNullOrWhiteSpace(model.Email) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                model.ErrorMessage = "Vui lòng điền đầy đủ Họ tên, Email và Mật khẩu!";
                return View("Login", model);
            }

            if (model.Password.Length < 6)
            {
                model.ErrorMessage = "Mật khẩu phải có tối thiểu 6 ký tự!";
                return View("Login", model);
            }

            if (model.Password != model.ConfirmPassword)
            {
                model.ErrorMessage = "Mật khẩu xác nhận không khớp!";
                return View("Login", model);
            }

            try
            {
                string emailInput = model.Email.Trim().ToLower();
                if (_db.Users.Any(u => u.Email.ToLower() == emailInput))
                {
                    model.ErrorMessage = "Email công việc này đã được đăng ký!";
                    return View("Login", model);
                }

                var newUser = new User
                {
                    FullName = model.FullName.Trim(),
                    Email = model.Email.Trim(),
                    PasswordHash = model.Password
                };

                _db.Users.Add(newUser);
                _db.SaveChanges();

                var defaultWs = _db.Workspaces.FirstOrDefault();
                if (defaultWs != null)
                {
                    _db.WorkspaceMembers.Add(new WorkspaceMember
                    {
                        WorkspaceID = defaultWs.WorkspaceID,
                        UserID = newUser.UserID,
                        Role = "Member"
                    });
                    _db.SaveChanges();
                }

                return RedirectToAction("Login", new { mode = "login", registered = "true" });
            }
            catch (Exception ex)
            {
                model.ErrorMessage = "Không thể tạo tài khoản: " + ex.Message;
                return View("Login", model);
            }
        }

        [HttpGet]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("TaskWeb_UserId");
            Response.Cookies.Delete("TaskWeb_UserName");
            Response.Cookies.Delete("TaskWeb_UserEmail");
            return RedirectToAction("Login");
        }
    }
}