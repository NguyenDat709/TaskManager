using System;
using System.Collections.Generic;

namespace TaskManager.Models
{
    public class KanbanViewModel
    {
        public string CurrentUserName { get; set; } = "Nguyễn Văn Quản Lý";
        public string CurrentWorkspaceName { get; set; } = "App Quản Lý Task";
        public string WorkspaceDescription { get; set; } = "Hệ thống quản lý công việc, phân bổ tiến độ và theo dõi Sprint thời gian thực.";
        public int CurrentWorkspaceId { get; set; } = 1;
        public int OverdueCount { get; set; }

        public List<Workspace> Workspaces { get; set; } = new();
        public List<User> Users { get; set; } = new();

        public List<TaskItemDto> TodoTasks { get; set; } = new();
        public List<TaskItemDto> InProgressTasks { get; set; } = new();
        public List<TaskItemDto> ReviewTasks { get; set; } = new();
        public List<TaskItemDto> DoneTasks { get; set; } = new();

        // Bộ lọc trên thanh công cụ
        public string? FilterSearch { get; set; }
        public string? FilterAssignee { get; set; }
        public string? FilterPriority { get; set; }
    }

    public class TaskItemDto
    {
        public int TaskID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "To-do";
        public string Priority { get; set; } = "Medium";
        public int? AssigneeID { get; set; }
        public string AssigneeName { get; set; } = "Chưa giao";
        public DateTime StartDate { get; set; }
        public DateTime Deadline { get; set; }
        public int SubtaskCount { get; set; }
        public int CompletedSubtaskCount { get; set; }
    }

    public class UpdateTaskStatusRequest
    {
        public int TaskId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}