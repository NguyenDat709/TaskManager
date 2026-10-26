using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Route("Gantt")]
    public class GanttController : Controller
    {
        private readonly AppDbContext _db;

        public GanttController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index(int workspaceId = 1, string? status = "All", string? search = null)
        {
            var now = DateTime.Now;
            //  hiển thị thời gian từ ngày 01 đến hết tháng hiện tại
            var timelineStart = new DateTime(now.Year, now.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            var timelineEnd = timelineStart.AddDays(daysInMonth - 1);

            var vm = new GanttViewModel
            {
                CurrentWorkspaceId = workspaceId,
                TimelineStart = timelineStart,
                TimelineEnd = timelineEnd,
                TotalDays = daysInMonth,
                FilterStatus = status,
                FilterSearch = search
            };

            List<TaskItem> rawTasks = new();
            List<TaskItem> subTasks = new();
            Dictionary<int, string> userDict = new();

            try
            {
                vm.Workspaces = _db.Workspaces.ToList();
                var currentWs = vm.Workspaces.FirstOrDefault(w => w.WorkspaceID == workspaceId)
                                ?? vm.Workspaces.FirstOrDefault();
                if (currentWs != null)
                {
                    vm.CurrentWorkspaceId = currentWs.WorkspaceID;
                    vm.CurrentWorkspaceName = currentWs.WorkspaceName;
                }

                userDict = _db.Users.ToDictionary(u => u.UserID, u => u.FullName);

                rawTasks = _db.Tasks
                    .Where(t => t.ParentTaskID == null && t.WorkspaceID == vm.CurrentWorkspaceId)
                    .OrderBy(t => t.StartDate)
                    .ToList();

                subTasks = _db.Tasks
                    .Where(t => t.ParentTaskID != null && t.WorkspaceID == vm.CurrentWorkspaceId)
                    .ToList();
            }
            catch
            {
                // data mẫu
                rawTasks = new List<TaskItem>
                {
                    new TaskItem { TaskID = 1, Title = "Task 1: Thiết kế giao diện Wireframe", Status = "Done", Priority = "Low", StartDate = timelineStart, Deadline = timelineStart.AddDays(3) },
                    new TaskItem { TaskID = 2, Title = "Task 2: Lập trình Backend API", Status = "In Progress", Priority = "Urgent", StartDate = timelineStart.AddDays(3), Deadline = timelineStart.AddDays(9) },
                    new TaskItem { TaskID = 3, Title = "Task 3: Viết Test Case & Kiểm thử", Status = "In Progress", Priority = "High", StartDate = timelineStart.AddDays(11), Deadline = timelineStart.AddDays(24) },
                    new TaskItem { TaskID = 4, Title = "Task 4: Tích hợp thông báo Real-time", Status = "To-do", Priority = "Medium", StartDate = timelineStart.AddDays(13), Deadline = timelineStart.AddDays(27) }
                };
            }

            // Lọc theo từ khóa và trạng thái
            if (!string.IsNullOrWhiteSpace(search))
            {
                rawTasks = rawTasks.Where(t => t.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                rawTasks = rawTasks.Where(t => t.Status == status).ToList();
            }

            // Quy đổi ngày tháng sang tọa độ cột trên biểu đồ Gantt
            foreach (var t in rawTasks)
            {
                var subs = subTasks.Where(s => s.ParentTaskID == t.TaskID).ToList();
                int subCount = subs.Count;
                int doneSubCount = subs.Count(s => s.Status == "Done");

                // Tính % tiến độ dựa trên Subtasks hoặc Status
                int progress = t.Status switch
                {
                    "Done" => 100,
                    "Review" => 80,
                    "In Progress" => subCount > 0 ? (int)Math.Round((double)doneSubCount / subCount * 100) : 50,
                    _ => 0
                };

                DateTime clampedStart = t.StartDate.Date < timelineStart ? timelineStart : t.StartDate.Date;
                DateTime clampedEnd = t.Deadline.Date > timelineEnd ? timelineEnd : t.Deadline.Date;
                if (clampedEnd < clampedStart) clampedEnd = clampedStart;

                int offset = (clampedStart - timelineStart).Days;
                int duration = (clampedEnd - clampedStart).Days + 1;
                if (offset + duration > daysInMonth) duration = daysInMonth - offset;
                if (duration < 1) duration = 1;

                vm.Tasks.Add(new GanttTaskDto
                {
                    TaskID = t.TaskID,
                    Title = t.Title,
                    Status = t.Status,
                    Priority = t.Priority,
                    AssigneeName = (t.AssigneeID.HasValue && userDict.ContainsKey(t.AssigneeID.Value))
                                    ? userDict[t.AssigneeID.Value]
                                    : "Chưa giao",
                    StartDate = t.StartDate,
                    Deadline = t.Deadline,
                    StartOffsetDays = Math.Clamp(offset, 0, daysInMonth - 1),
                    DurationDays = duration,
                    ProgressPercent = progress,
                    IsOverdue = t.Status != "Done" && t.Deadline < now,
                    SubtaskCount = subCount,
                    DoneSubtaskCount = doneSubCount
                });
            }

            vm.TotalTasks = vm.Tasks.Count;
            vm.CompletedTasks = vm.Tasks.Count(t => t.Status == "Done");
            vm.OverdueTasks = vm.Tasks.Count(t => t.IsOverdue);

            return View(vm);
        }

        // API cập nhật StartDate & Deadline xuống CSDL 
        [HttpPost]
        [Route("UpdateSchedule")]
        public IActionResult UpdateSchedule([FromBody] UpdateTaskScheduleRequest req)
        {
            if (req == null || req.TaskId <= 0)
                return BadRequest(new { success = false, message = "Dữ liệu công việc không hợp lệ!" });

            // Hạn chót không được phép nhỏ hơn Ngày bắt đầu
            if (req.Deadline < req.StartDate)
                return BadRequest(new { success = false, message = "Hạn chót (Deadline) phải lớn hơn hoặc bằng Ngày bắt đầu!" });

            try
            {
                var task = _db.Tasks.FirstOrDefault(t => t.TaskID == req.TaskId);
                if (task == null)
                    return NotFound(new { success = false, message = "Không tìm thấy công việc trong CSDL SQL Server!" });

                task.StartDate = req.StartDate;
                task.Deadline = req.Deadline;
                _db.SaveChanges();

                return Ok(new
                {
                    success = true,
                    message = "Đã cập nhật lịch trình vào CSDL SQL Server!",
                    taskId = task.TaskID,
                    startDate = task.StartDate.ToString("yyyy-MM-ddTHH:mm"),
                    deadline = task.Deadline.ToString("yyyy-MM-ddTHH:mm")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi CSDL SQL Server: " + ex.Message });
            }
        }
    }
}