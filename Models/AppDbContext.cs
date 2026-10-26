using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.Models
{
   //entity model

    [Table("Users")]
    public class User
    {
        [Key] public int UserID { get; set; }
        public Guid UserGUID { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? AvatarURL { get; set; }
        public bool IsEmailConfirmed { get; set; } = false;
        public string Status { get; set; } = "Active";
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("Workspaces")]
    public class Workspace
    {
        [Key] public int WorkspaceID { get; set; }
        public Guid WorkspaceGUID { get; set; } = Guid.NewGuid();
        public string WorkspaceName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OwnerID { get; set; }
        public string? InviteCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DueDate { get; set; }
    }

    [Table("WorkspaceMembers")]
    public class WorkspaceMember
    {
        public int WorkspaceID { get; set; }
        public int UserID { get; set; }
        public string Role { get; set; } = "Member";
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }

    [Table("Tasks")]
    public class TaskItem
    {
        [Key] public int TaskID { get; set; }
        public Guid TaskGUID { get; set; } = Guid.NewGuid();
        public int WorkspaceID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Priority { get; set; } = "Medium";
        public int? AssigneeID { get; set; }
        public int? ParentTaskID { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime Deadline { get; set; } = DateTime.Now.AddDays(7);

        public string Status { get; set; } = "To-do";
        public DateTime? StatusChangedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("TaskAssignees")]
    public class TaskAssignee
    {
        public int TaskID { get; set; }
        public int UserID { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.Now;
    }

    [Table("TaskChecklists")]
    public class TaskChecklist
    {
        [Key] public int ChecklistID { get; set; }
        public int TaskID { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("TaskDependencies")]
    public class TaskDependency
    {
        [Key] public int DependencyID { get; set; }
        public int PredecessorTaskID { get; set; }
        public int SuccessorTaskID { get; set; }
        public string DependencyType { get; set; } = "FS";
    }

    [Table("TaskTags")]
    public class TaskTag
    {
        [Key] public int TagID { get; set; }
        public int WorkspaceID { get; set; }
        public string TagName { get; set; } = string.Empty;
        public string ColorCode { get; set; } = "#6c757d";
    }

    [Table("TaskTagMappings")]
    public class TaskTagMapping
    {
        public int TaskID { get; set; }
        public int TagID { get; set; }
    }

    [Table("Comments")]
    public class Comment
    {
        [Key] public int CommentID { get; set; }
        public Guid CommentGUID { get; set; } = Guid.NewGuid();
        public int TaskID { get; set; }
        public int UserID { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("Attachments")]
    public class Attachment
    {
        [Key] public int AttachmentID { get; set; }
        public Guid AttachmentGUID { get; set; } = Guid.NewGuid();
        public int TaskID { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FileURL { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public int UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }

    [Table("TaskHistories")]
    public class TaskHistory
    {
        [Key] public int HistoryID { get; set; }
        public int TaskID { get; set; }
        public int UserID { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("Notifications")]
    public class Notification
    {
        [Key] public int NotificationID { get; set; }
        public int UserID { get; set; }
        public string? Title { get; set; }
        public string Content { get; set; } = string.Empty;
        public string NotificationType { get; set; } = "SYSTEM";
        public bool IsRead { get; set; } = false;
        public int? ReferenceTaskID { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key] public int LogID { get; set; }
        public int? UserID { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public string? EntityID { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    //vmodel
    public class HomeDashboardViewModel
    {
        public int CurrentUserId { get; set; }
        public int CurrentWorkspaceId { get; set; }
        public string CurrentUserName { get; set; } = "Thành viên";
        public string CurrentWorkspaceName { get; set; } = "Dự án mặc định của tôi";
        public string? FilterSearch { get; set; }
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

    public class TaskDetailViewModel
    {
        public TaskItem Task { get; set; } = new();
        public Workspace Workspace { get; set; } = new();
        public User? Assignee { get; set; }
        public List<User> Members { get; set; } = new();
        public List<TaskChecklist> Checklists { get; set; } = new();
        public List<Attachment> Attachments { get; set; } = new();
        public List<CommentDetailDto> Comments { get; set; } = new();
    }

    public class CommentDetailDto
    {
        public int CommentID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

  // dbcontext
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Workspace> Workspaces { get; set; }
        public DbSet<WorkspaceMember> WorkspaceMembers { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<TaskAssignee> TaskAssignees { get; set; }
        public DbSet<TaskChecklist> TaskChecklists { get; set; }
        public DbSet<TaskDependency> TaskDependencies { get; set; }
        public DbSet<TaskTag> TaskTags { get; set; }
        public DbSet<TaskTagMapping> TaskTagMappings { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<TaskHistory> TaskHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Workspace>().ToTable("Workspaces");

            modelBuilder.Entity<WorkspaceMember>().ToTable("WorkspaceMembers")
                .HasKey(wm => new { wm.WorkspaceID, wm.UserID });

            modelBuilder.Entity<TaskItem>().ToTable("Tasks");

            modelBuilder.Entity<TaskAssignee>().ToTable("TaskAssignees")
                .HasKey(ta => new { ta.TaskID, ta.UserID });

            modelBuilder.Entity<TaskChecklist>().ToTable("TaskChecklists");
            modelBuilder.Entity<TaskDependency>().ToTable("TaskDependencies");
            modelBuilder.Entity<TaskTag>().ToTable("TaskTags");

            modelBuilder.Entity<TaskTagMapping>().ToTable("TaskTagMappings")
                .HasKey(ttm => new { ttm.TaskID, ttm.TagID });

            modelBuilder.Entity<Comment>().ToTable("Comments");
            modelBuilder.Entity<Attachment>().ToTable("Attachments");
            modelBuilder.Entity<TaskHistory>().ToTable("TaskHistories");
            modelBuilder.Entity<Notification>().ToTable("Notifications");
            modelBuilder.Entity<AuditLog>().ToTable("AuditLogs");
        }
    }
}