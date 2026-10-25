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

        // Hàm hỗ trợ lấy UserID an toàn từ Session hoặc Cookie
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

        // 1. TRANG CHỦ DASHBOARD
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

            var tasks = _context.Tasks.Where(t => t.WorkspaceID == activeWorkspaceId).OrderByDescending(t => t.TaskID).ToList();
            DateTime now = DateTime.Now;

            // ĐÓNG GÓI DỮ LIỆU VÀO VIEWMODEL
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

            return View(viewModel); // TRUYỀN VIEWMODEL VÀO VIEW
        }

        // 2. ACTION AJAX: TRẢ VỀ PARTIAL VIEW ĐỔI CHẾ ĐỘ XEM
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

        // 3. TẠO DỰ ÁN MỚI (WORKSPACE)
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

        // 4. TẠO CÔNG VIỆC MỚI (TASK)
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
                Status = "To do",
                CreatedAt = DateTime.Now
            };

            _context.Tasks.Add(newTask);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Thêm công việc mới thành công!";
            return RedirectToAction("Index", new { workspaceId });
        }

        // 5. CẬP NHẬT TRẠNG THÁI TASK
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

        // 6. XÓA CÔNG VIỆC (TASK)
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

        // 7. THAM GIA DỰ ÁN BẰNG MÃ MỜI (INVITE CODE)
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
    }
}