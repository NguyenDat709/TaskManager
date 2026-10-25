using System;
using System.Collections.Generic;

namespace TaskManager.Models
{
    public class GanttViewModel
    {
        public string CurrentUserName { get; set; } = "Nguyễn Văn Đạt";
        public int CurrentWorkspaceId { get; set; } = 1;
        public string CurrentWorkspaceName { get; set; } = "Thiết kế E-Commerce";
        public string? FilterSearch { get; set; }
        public int OverdueCount => OverdueTasks; 
        public List<Workspace> Workspaces { get; set; } = new();

        public DateTime TimelineStart { get; set; }
        public DateTime TimelineEnd { get; set; }
        public int TotalDays { get; set; }

        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }

        public string? FilterStatus { get; set; }
        public List<GanttTaskDto> Tasks { get; set; } = new();
    }

    public class GanttTaskDto
    {
        public int TaskID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = "To-do";
        public string Priority { get; set; } = "Medium";
        public string AssigneeName { get; set; } = "Chưa giao";
        public DateTime StartDate { get; set; }
        public DateTime Deadline { get; set; }

        public int StartOffsetDays { get; set; }
        public int DurationDays { get; set; }
        public int ProgressPercent { get; set; }
        public bool IsOverdue { get; set; }
        public int SubtaskCount { get; set; }
        public int DoneSubtaskCount { get; set; }
    }

    public class UpdateTaskScheduleRequest
    {
        public int TaskId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime Deadline { get; set; }
    }
}