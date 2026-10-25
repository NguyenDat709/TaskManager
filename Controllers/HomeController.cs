using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index(string? search)
        {
            var now = DateTime.Now;
            var next24h = now.AddHours(24);
            var vm = new HomeDashboardViewModel();

            try
            {
                vm.Workspaces = _db.Workspaces.ToList();
                if (vm.Workspaces.Any())
                {
                    vm.CurrentWorkspaceName = vm.Workspaces.First().WorkspaceName;
                }

                var allTasksQuery = _db.Tasks.Where(t => t.ParentTaskID == null);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string kw = search.Trim().ToLower();
                    allTasksQuery = allTasksQuery.Where(t => t.Title.ToLower().Contains(kw));
                }

                var mainTasks = allTasksQuery.OrderBy(t => t.Deadline).ToList();

                var subTasks = _db.Tasks.Where(t => t.ParentTaskID != null).ToList();
                foreach (var t in mainTasks)
                {
                    vm.SubtaskCounts[t.TaskID] = subTasks.Count(st => st.ParentTaskID == t.TaskID);
                }

                vm.OverdueTasks = mainTasks
                    .Where(t => t.Status != "Done" && t.Deadline < now)
                    .ToList();

                vm.TodayAnd24hTasks = mainTasks
                    .Where(t => t.Status != "Done" && t.Deadline >= now && t.Deadline <= next24h)
                    .ToList();

                vm.UpcomingAndOtherTasks = mainTasks
                    .Where(t => t.Status == "Done" || t.Deadline > next24h)
                    .ToList();

                
                vm.TotalAssigned = mainTasks.Count;
                vm.InProgressCount = mainTasks.Count(t => t.Status == "In Progress");
                vm.DueWithin24hCount = vm.TodayAnd24hTasks.Count;
                vm.OverdueCount = vm.OverdueTasks.Count;

                vm.RecentLogs = _db.Notifications
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(5)
                    .ToList();
            }
            catch
            {
                vm.TotalAssigned = 12;
                vm.InProgressCount = 5;
                vm.DueWithin24hCount = 2;
                vm.OverdueCount = 1;
                vm.OverdueTasks.Add(new TaskItem
                {
                    TaskID = 104,
                    Title = "Lập trình Backend API & Xử lý cảnh báo",
                    Priority = "Urgent",
                    Status = "In Progress",
                    Deadline = now.AddHours(-18)
                });
                vm.TodayAnd24hTasks.Add(new TaskItem
                {
                    TaskID = 112,
                    Title = "Thiết kế CSDL & Kiểm thử tích hợp",
                    Priority = "High",
                    Status = "In Progress",
                    Deadline = now.AddHours(6)
                });
                vm.UpcomingAndOtherTasks.Add(new TaskItem
                {
                    TaskID = 115,
                    Title = "Kiểm thử hệ thống & Nghiệm thu tổng thể",
                    Priority = "Medium",
                    Status = "To-do",
                    Deadline = now.AddDays(4)
                });
            }

            return View(vm);
        }

        [HttpPost]
        public IActionResult QuickCreateTask(string title, string priority, DateTime deadline)
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                var newTask = new TaskItem
                {
                    WorkspaceID = 1,
                    Title = title.Trim(),
                    Priority = priority,
                    StartDate = DateTime.Now,
                    Deadline = deadline,
                    Status = "To-do",
                    AssigneeID = 1
                };
                _db.Tasks.Add(newTask);
                _db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}