using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Route("Workspace")]
    public class WorkspaceController : Controller
    {
        private readonly AppDbContext _context;

        public WorkspaceController(AppDbContext context)
        {
            _context = context;
        }

        // Hàm lấy iduser từ session 
        private int? GetCurrentUserId()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                string? cookieUserId = Request.Cookies["TaskWeb_UserId"];
                if (!string.IsNullOrEmpty(cookieUserId) && int.TryParse(cookieUserId, out int parsedId))
                {
                    userId = parsedId;
                    HttpContext.Session.SetInt32("UserID", userId.Value); // Đồng bộ lại vào Session
                }
            }
            return userId;
        }

        // trang chủ

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index(int? workspaceId, string viewMode = "todo")
        {
            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var myWorkspaces = _context.WorkspaceMembers
                .Where(wm => wm.UserID == currentUserId.Value)
                .Join(_context.Workspaces,
                    wm => wm.WorkspaceID,
                    w => w.WorkspaceID,
                    (wm, w) => new { Workspace = w, Role = wm.Role })
                .ToList();

            if (!myWorkspaces.Any())
            {
                var defaultWorkspace = new Workspace
                {
                    WorkspaceName = "Dự án đầu tiên của tôi",
                    Description = "Không gian làm việc khởi tạo tự động",
                    OwnerID = currentUserId.Value,
                    InviteCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
                };
                _context.Workspaces.Add(defaultWorkspace);
                _context.SaveChanges();

                var memberRole = new WorkspaceMember
                {
                    WorkspaceID = defaultWorkspace.WorkspaceID,
                    UserID = currentUserId.Value,
                    Role = "Owner"
                };
                _context.WorkspaceMembers.Add(memberRole);
                _context.SaveChanges();

                return RedirectToAction("Index", new { workspaceId = defaultWorkspace.WorkspaceID });
            }

            int activeWorkspaceId = workspaceId ?? myWorkspaces.First().Workspace.WorkspaceID;
            var activeWorkspaceInfo = myWorkspaces.FirstOrDefault(w => w.Workspace.WorkspaceID == activeWorkspaceId);

            if (activeWorkspaceInfo == null)
            {
                return NotFound("Không tìm thấy dự án hoặc bạn không có quyền truy cập.");
            }

            // lưu role của user hiện tại lưu vào viewbag
            ViewBag.UserRole = activeWorkspaceInfo.Role;
            ViewBag.InviteCode = activeWorkspaceInfo.Workspace.InviteCode;

            var tasks = _context.Tasks.Where(t => t.WorkspaceID == activeWorkspaceId).OrderByDescending(t => t.TaskID).ToList();
            DateTime now = DateTime.Now;

            // đóng gói dl vào vmodel
            var viewModel = new HomeDashboardViewModel
            {
                CurrentUserId = currentUserId.Value,
                CurrentUserName = Request.Cookies["TaskWeb_UserName"] ?? "Bạn",
                CurrentWorkspaceId = activeWorkspaceId,
                CurrentWorkspaceName = activeWorkspaceInfo.Workspace.WorkspaceName,
                Workspaces = myWorkspaces.Select(w => w.Workspace).ToList(), // Truyền danh sách dự án sang Sidebar
                TotalAssigned = tasks.Count,
                InProgressCount = tasks.Count(t => t.Status == "In Progress"),
                DueWithin24hCount = tasks.Count(t => t.Deadline > now && t.Deadline <= now.AddHours(24) && t.Status != "Done"),
                OverdueCount = tasks.Count(t => t.Deadline < now && t.Status != "Done"),
                OverdueTasks = tasks.Where(t => t.Deadline < now && t.Status != "Done").ToList(),
                TodayAnd24hTasks = tasks.Where(t => t.Deadline >= now && t.Deadline <= now.AddHours(24) && t.Status != "Done").ToList(),
                UpcomingAndOtherTasks = tasks.Where(t => (t.Deadline > now.AddHours(24) && t.Status != "Done") || t.Status == "Done").ToList()
            };

            return View(viewModel);
        }

        //  ajax trả về patial view để đổi chế độ xem
        [HttpGet]
        [Route("GetViewModePartial")]
        public IActionResult GetViewModePartial(int workspaceId, string mode)
        {
            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var tasks = _context.Tasks.Where(t => t.WorkspaceID == workspaceId).OrderByDescending(t => t.TaskID).ToList();
            ViewBag.Tasks = tasks;

            if (mode == "calendar") return PartialView("_CalendarPartial");
            return PartialView("_TodoListPartial");
        }

        // tạo dự án mới
        [HttpPost]
        [Route("Create")]
        public IActionResult Create(string workspaceName, string description)
        {
            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(workspaceName))
            {
                TempData["ErrorMessage"] = "Tên dự án không được để trống.";
                return RedirectToAction("Index");
            }

            var workspace = new Workspace
            {
                WorkspaceName = workspaceName.Trim(),
                Description = description?.Trim(),
                OwnerID = currentUserId.Value,
                InviteCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
            };

            _context.Workspaces.Add(workspace);
            _context.SaveChanges();

            var member = new WorkspaceMember
            {
                WorkspaceID = workspace.WorkspaceID,
                UserID = currentUserId.Value,
                Role = "Owner"
            };

            _context.WorkspaceMembers.Add(member);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Đã tạo thành công dự án mới '{workspace.WorkspaceName}'!";
            return RedirectToAction("Index", new { workspaceId = workspace.WorkspaceID });
        }

        // tạo task mới
        [HttpPost]
        [Route("CreateTask")]
        public IActionResult CreateTask(int workspaceId, string title, string? description, DateTime? deadline, string priority)
        {
            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "Tên công việc không được để trống!";
                return RedirectToAction("Index", new { workspaceId });
            }

            var newTask = new TaskItem
            {
                WorkspaceID = workspaceId,
                Title = title.Trim(),
                Description = description?.Trim(),
                Deadline = deadline ?? DateTime.Now.AddDays(7),
                Priority = string.IsNullOrEmpty(priority) ? "Medium" : priority,
                Status = "To-do",
                CreatedAt = DateTime.Now
            };

            _context.Tasks.Add(newTask);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thêm công việc mới thành công!";
            return RedirectToAction("Index", new { workspaceId });
        }

        // cập nhật trạng thái task
        [HttpPost]
        [Route("UpdateTaskStatus")]
        public IActionResult UpdateTaskStatus(int taskId, string status, int workspaceId)
        {
            var task = _context.Tasks.FirstOrDefault(t => t.TaskID == taskId);
            if (task != null)
            {
                task.Status = status;
                task.StatusChangedAt = DateTime.Now;
                _context.SaveChanges();
            }
            return RedirectToAction("Index", new { workspaceId });
        }

        // xóa task
        [HttpPost]
        [Route("DeleteTask")]
        public IActionResult DeleteTask(int taskId, int workspaceId)
        {
            var task = _context.Tasks.FirstOrDefault(t => t.TaskID == taskId);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                _context.SaveChanges();
            }
            return RedirectToAction("Index", new { workspaceId });
        }

        // join dự án = mã mời
        [HttpPost]
        [Route("JoinWorkspace")]
        public IActionResult JoinWorkspace(string inviteCode)
        {
            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(inviteCode))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã mời!";
                return RedirectToAction("Index");
            }

            var workspace = _context.Workspaces.FirstOrDefault(w => w.InviteCode == inviteCode.Trim().ToUpper());
            if (workspace == null)
            {
                TempData["ErrorMessage"] = "Mã mời không tồn tại!";
                return RedirectToAction("Index");
            }

            bool isAlreadyMember = _context.WorkspaceMembers.Any(wm => wm.WorkspaceID == workspace.WorkspaceID && wm.UserID == currentUserId.Value);
            if (isAlreadyMember)
            {
                TempData["ErrorMessage"] = "Bạn đã là thành viên của dự án này rồi!";
                return RedirectToAction("Index", new { workspaceId = workspace.WorkspaceID });
            }

            var newMember = new WorkspaceMember
            {
                WorkspaceID = workspace.WorkspaceID,
                UserID = currentUserId.Value,
                Role = "Member"
            };
            _context.WorkspaceMembers.Add(newMember);
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Gia nhập dự án '{workspace.WorkspaceName}' thành công!";
            return RedirectToAction("Index", new { workspaceId = workspace.WorkspaceID });
        }

        // mời thành viên qua mail
        [HttpPost]
        [Route("InviteMember")]
        public IActionResult InviteMember(int workspaceId, string email, string role = "Member")
        {
            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var currentMember = _context.WorkspaceMembers.FirstOrDefault(wm => wm.WorkspaceID == workspaceId && wm.UserID == currentUserId.Value);
            if (currentMember == null || currentMember.Role != "Owner")
            {
                return BadRequest(new { success = false, message = "Chỉ Trưởng dự án (Owner) mới có quyền mời và phân quyền thành viên!" });
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập địa chỉ Email!" });
            }

            string cleanEmail = email.Trim().ToLower();

            var userToInvite = _context.Users.FirstOrDefault(u => u.Email.ToLower() == cleanEmail);
            if (userToInvite == null)
            {
                var workspace = _context.Workspaces.FirstOrDefault(w => w.WorkspaceID == workspaceId);
                string code = workspace?.InviteCode ?? "";
                return Ok(new
                {
                    success = true,
                    isNewUser = true,
                    message = $"Email '{cleanEmail}' chưa có tài khoản trên hệ thống.\n\nBạn hãy gửi Mã mời [{code}] này để người nhận đăng ký và gia nhập dự án nhé!"
                });
            }

            bool isAlreadyMember = _context.WorkspaceMembers.Any(wm => wm.WorkspaceID == workspaceId && wm.UserID == userToInvite.UserID);
            if (isAlreadyMember)
            {
                return BadRequest(new { success = false, message = "Thành viên này đã tham gia vào dự án này rồi!" });
            }

            string assignedRole = (role == "Viewer") ? "Viewer" : "Member";

            var newMember = new WorkspaceMember
            {
                WorkspaceID = workspaceId,
                UserID = userToInvite.UserID,
                Role = assignedRole,
                JoinedAt = DateTime.Now
            };

            _context.WorkspaceMembers.Add(newMember);
            _context.SaveChanges();

            return Ok(new
            {
                success = true,
                isNewUser = false,
                message = $"Đã thêm thành công {userToInvite.FullName} vào dự án với vai trò {assignedRole}!"
            });
        }

        // thay đổi vai trò tv
        [HttpPost]
        [Route("UpdateMemberRole")]
        public IActionResult UpdateMemberRole(int workspaceId, int targetUserId, string newRole)
        {
            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var currentMember = _context.WorkspaceMembers.FirstOrDefault(wm => wm.WorkspaceID == workspaceId && wm.UserID == currentUserId.Value);
            if (currentMember == null || currentMember.Role != "Owner")
            {
                return BadRequest(new { success = false, message = "Bạn không có quyền thay đổi vai trò thành viên!" });
            }

            var targetMember = _context.WorkspaceMembers.FirstOrDefault(wm => wm.WorkspaceID == workspaceId && wm.UserID == targetUserId);
            if (targetMember != null)
            {
                if (targetMember.Role == "Owner")
                {
                    return BadRequest(new { success = false, message = "Không thể thay đổi vai trò của Chủ dự án!" });
                }

                targetMember.Role = (newRole == "Viewer") ? "Viewer" : "Member";
                _context.SaveChanges();
                return Ok(new { success = true });
            }

            return NotFound();
        }

        // xóa tv
        [HttpPost]
        [Route("RemoveMember")]
        public IActionResult RemoveMember(int workspaceId, int targetUserId)
        {
            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var currentMember = _context.WorkspaceMembers.FirstOrDefault(wm => wm.WorkspaceID == workspaceId && wm.UserID == currentUserId.Value);
            if (currentMember == null || currentMember.Role != "Owner")
            {
                return BadRequest(new { success = false, message = "Chỉ Trưởng dự án mới có quyền xóa thành viên!" });
            }

            var targetMember = _context.WorkspaceMembers.FirstOrDefault(wm => wm.WorkspaceID == workspaceId && wm.UserID == targetUserId);
            if (targetMember != null)
            {
                if (targetMember.Role == "Owner")
                {
                    return BadRequest(new { success = false, message = "Không thể xóa Trưởng dự án khỏi Workspace!" });
                }

                _context.WorkspaceMembers.Remove(targetMember);
                _context.SaveChanges();
                return Ok(new { success = true });
            }

            return NotFound();
        }

        // xóa dự án
        [HttpPost]
        [Route("Delete")]
        public IActionResult Delete(int workspaceId)
        {
            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var workspace = _context.Workspaces.FirstOrDefault(w => w.WorkspaceID == workspaceId);
            if (workspace == null)
            {
                return NotFound("Không tìm thấy dự án!");
            }

            if (workspace.OwnerID != currentUserId.Value)
            {
                TempData["ErrorMessage"] = "Chỉ Trưởng dự án mới có quyền xóa dự án này!";
                return RedirectToAction("Index", new { workspaceId });
            }

            // Xóa các dữ liệu phụ thuộc trong DB
            var tasks = _context.Tasks.Where(t => t.WorkspaceID == workspaceId).ToList();
            var taskIds = tasks.Select(t => t.TaskID).ToList();

            var checklists = _context.TaskChecklists.Where(c => taskIds.Contains(c.TaskID)).ToList();
            var attachments = _context.Attachments.Where(a => taskIds.Contains(a.TaskID)).ToList();
            var comments = _context.Comments.Where(cm => taskIds.Contains(cm.TaskID)).ToList();

            _context.TaskChecklists.RemoveRange(checklists);
            _context.Attachments.RemoveRange(attachments);
            _context.Comments.RemoveRange(comments);
            _context.Tasks.RemoveRange(tasks);

            var members = _context.WorkspaceMembers.Where(wm => wm.WorkspaceID == workspaceId).ToList();
            _context.WorkspaceMembers.RemoveRange(members);
            _context.Workspaces.Remove(workspace);

            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Đã xóa thành công dự án '{workspace.WorkspaceName}'!";
            return RedirectToAction("Index");
        }
    }
}