using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.Models
{
    public class User
    {
        [Key] public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }

    public class Workspace
    {
        [Key] public int WorkspaceID { get; set; }
        public string WorkspaceName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OwnerID { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class WorkspaceMember
    {
        public int WorkspaceID { get; set; }
        public int UserID { get; set; }
        public string Role { get; set; } = "Member";
    }

    public class TaskItem
    {
        [Key] public int TaskID { get; set; }
        public int WorkspaceID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "Medium";
        public int? AssigneeID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime Deadline { get; set; }
        public string Status { get; set; } = "To-do";
        public int? ParentTaskID { get; set; }
    }

    public class Notification
    {
        [Key] public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class HomeDashboardViewModel
    {
        public string CurrentUserName { get; set; } = "Nguyễn Văn Đạt";
        public string CurrentWorkspaceName { get; set; } = "Thiết kế E-Commerce";
        
        public int TotalAssigned { get; set; }
        public int InProgressCount { get; set; }
        public int DueWithin24hCount { get; set; }
        public int OverdueCount { get; set; }

        public List<TaskItem> OverdueTasks { get; set; } = new();
        public List<TaskItem> TodayAnd24hTasks { get; set; } = new();
        public List<TaskItem> UpcomingAndOtherTasks { get; set; } = new();

        public List<Workspace> Workspaces { get; set; } = new();
        public List<Notification> RecentLogs { get; set; } = new();
        public Dictionary<int, int> SubtaskCounts { get; set; } = new();
    }

    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Workspace>().ToTable("Workspaces");
            modelBuilder.Entity<WorkspaceMember>().ToTable("WorkspaceMembers")
                .HasKey(wm => new { wm.WorkspaceID, wm.UserID });
            modelBuilder.Entity<TaskItem>().ToTable("Tasks");
            modelBuilder.Entity<Notification>().ToTable("Notifications");
        }
    }
}