using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Route("Kanban")]
    public class KanbanController : Controller
    {
        private readonly AppDbContext _db;

        public KanbanController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index(string? search, string? assignee, string? priority, int workspaceId = 1)
        {
            var vm = new KanbanViewModel
            {
                FilterSearch = search,
                FilterAssignee = assignee,
                FilterPriority = priority,
                CurrentWorkspaceId = workspaceId
            };

            List<TaskItemDto> taskDtos = new();

            try
            {
                //  Lấy danh sách Dự án và Người dùng từ SQL 
                vm.Workspaces = _db.Workspaces.ToList();
                vm.Users = _db.Users.ToList();

                var currentWs = vm.Workspaces.FirstOrDefault(w => w.WorkspaceID == workspaceId)
                                ?? vm.Workspaces.FirstOrDefault();
                if (currentWs != null)
                {
                    vm.CurrentWorkspaceId = currentWs.WorkspaceID;
                    vm.CurrentWorkspaceName = currentWs.WorkspaceName;
                    vm.WorkspaceDescription = currentWs.Description ?? vm.WorkspaceDescription;
                }

                var userDict = vm.Users.ToDictionary(u => u.UserID, u => u.FullName);

                //  Lấy toàn bộ Task cha thuộc Workspace hiện tại
                var mainTasks = _db.Tasks
                    .Where(t => t.ParentTaskID == null && t.WorkspaceID == vm.CurrentWorkspaceId)
                    .OrderBy(t => t.Deadline)
                    .ToList();

                // Lấy Subtasks để đếm tiến độ checklist trên mỗi thẻ 
                var subTasks = _db.Tasks
                    .Where(t => t.ParentTaskID != null && t.WorkspaceID == vm.CurrentWorkspaceId)
                    .ToList();

                foreach (var t in mainTasks)
                {
                    var subs = subTasks.Where(st => st.ParentTaskID == t.TaskID).ToList();
                    taskDtos.Add(new TaskItemDto
                    {
                        TaskID = t.TaskID,
                        Title = t.Title,
                        Description = t.Description,
                        Status = (t.Status == "To Do" || string.IsNullOrEmpty(t.Status)) ? "To-do" : t.Status,
                        Priority = t.Priority,
                        AssigneeID = t.AssigneeID,
                        AssigneeName = (t.AssigneeID.HasValue && userDict.ContainsKey(t.AssigneeID.Value))
                                        ? userDict[t.AssigneeID.Value]
                                        : "Chưa giao",
                        StartDate = t.StartDate,
                        Deadline = t.Deadline,
                        SubtaskCount = subs.Count,
                        CompletedSubtaskCount = subs.Count(s => s.Status == "Done")
                    });
                }
            }
            catch
            {
                // data mẫu
                taskDtos = new List<TaskItemDto>
                {
                    new TaskItemDto { TaskID = 1, Title = "Task 1: Thiết kế giao diện Wireframe", Status = "Done", Priority = "Low", AssigneeName = "Phạm Minh", Deadline = DateTime.Now.AddDays(-5), SubtaskCount = 0 },
                    new TaskItemDto { TaskID = 2, Title = "Task 2: Lập trình Backend API", Status = "In Progress", Priority = "Urgent", AssigneeName = "Trần Huy", Deadline = DateTime.Now.AddDays(-2), SubtaskCount = 2, CompletedSubtaskCount = 1 },
                    new TaskItemDto { TaskID = 3, Title = "Task 3: Viết Test Case & Kiểm thử", Status = "Review", Priority = "High", AssigneeName = "Lê Toàn", Deadline = DateTime.Now.AddDays(1), SubtaskCount = 2, CompletedSubtaskCount = 1 },
                    new TaskItemDto { TaskID = 4, Title = "Task 4: Tích hợp thông báo Real-time", Status = "To-do", Priority = "Medium", AssigneeName = "Trần Huy", Deadline = DateTime.Now.AddDays(3), SubtaskCount = 0 }
                };
            }

            // lọc
            var query = taskDtos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => t.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                                      || t.TaskID.ToString().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(assignee) && assignee != "All")
            {
                query = query.Where(t => t.AssigneeName == assignee);
            }

            if (!string.IsNullOrWhiteSpace(priority) && priority != "All")
            {
                query = query.Where(t => t.Priority == priority);
            }

            var filteredTasks = query.ToList();

            // 4 cột trạng thái của task
            vm.OverdueCount = filteredTasks.Count(t => t.Deadline < DateTime.Now && t.Status != "Done");
            vm.TodoTasks = filteredTasks.Where(t => t.Status == "To-do").ToList();
            vm.InProgressTasks = filteredTasks.Where(t => t.Status == "In Progress").ToList();
            vm.ReviewTasks = filteredTasks.Where(t => t.Status == "Review").ToList();
            vm.DoneTasks = filteredTasks.Where(t => t.Status == "Done").ToList();

            return View(vm);
        }

        // API cập nhật trạng thái khi kéo thả thẻ 
        [HttpPost]
        [Route("UpdateStatus")]
        public IActionResult UpdateStatus([FromBody] UpdateTaskStatusRequest req)
        {
            if (req == null || req.TaskId <= 0 || string.IsNullOrWhiteSpace(req.NewStatus))
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            try
            {
                var dbTask = _db.Tasks.FirstOrDefault(t => t.TaskID == req.TaskId);
                if (dbTask == null)
                    return NotFound(new { success = false, message = "Không tìm thấy công việc trong CSDL SQL Server!" });

                dbTask.Status = req.NewStatus;
                _db.SaveChanges();

                return Ok(new { success = true, taskId = dbTask.TaskID, newStatus = dbTask.Status });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi lưu CSDL SQL Server: " + ex.Message });
            }
        }

        // Tạo mới công việc trực tiếp từ trang Kanban
        [HttpPost]
        [Route("CreateTask")]
        public IActionResult CreateTask(string title, string? description, string status, string priority, int? assigneeId, DateTime deadline, int workspaceId = 1)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                var newTask = new TaskItem
                {
                    WorkspaceID = workspaceId,
                    Title = title.Trim(),
                    Description = description,
                    Status = string.IsNullOrWhiteSpace(status) ? "To-do" : status,
                    Priority = string.IsNullOrWhiteSpace(priority) ? "Medium" : priority,
                    AssigneeID = assigneeId,
                    StartDate = DateTime.Now,
                    Deadline = deadline == default ? DateTime.Now.AddDays(3) : deadline
                };

                _db.Tasks.Add(newTask);
                _db.SaveChanges();
            }

            return RedirectToAction("Index", new { workspaceId });
        }
    }
}