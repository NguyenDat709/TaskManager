using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    [Route("TaskDetail")]
    public class TaskDetailController : Controller
    {
        private readonly AppDbContext _db;

        public TaskDetailController(AppDbContext db)
        {
            _db = db;
        }

        private int? GetCurrentUserId()
        {
            int? userId = HttpContext.Session.GetInt32("UserID");
            if (userId == null)
            {
                string? cookieUserId = Request.Cookies["TaskWeb_UserId"];
                if (!string.IsNullOrEmpty(cookieUserId) && int.TryParse(cookieUserId, out int parsedId))
                {
                    userId = parsedId;
                    HttpContext.Session.SetInt32("UserID", userId.Value);
                }
            }
            return userId;
        }

        // lấy chi tiết task
        [HttpGet]
        [Route("GetDetailPartial/{taskId}")]
        public IActionResult GetDetailPartial(int taskId)
        {
            var task = _db.Tasks.FirstOrDefault(t => t.TaskID == taskId);
            if (task == null) return NotFound("Không tìm thấy công việc!");

            var workspace = _db.Workspaces.FirstOrDefault(w => w.WorkspaceID == task.WorkspaceID) ?? new Workspace();
            var assignee = task.AssigneeID.HasValue ? _db.Users.FirstOrDefault(u => u.UserID == task.AssigneeID.Value) : null;

            var members = _db.WorkspaceMembers
                .Where(wm => wm.WorkspaceID == task.WorkspaceID)
                .Join(_db.Users, wm => wm.UserID, u => u.UserID, (wm, u) => u)
                .ToList();

            var checklists = _db.TaskChecklists.Where(c => c.TaskID == taskId).OrderBy(c => c.ChecklistID).ToList();
            var attachments = _db.Attachments.Where(a => a.TaskID == taskId).OrderByDescending(a => a.UploadedAt).ToList();

            //lấy tên của user để hiển thị cmt
            var comments = _db.Comments
                .Where(c => c.TaskID == taskId)
                .OrderByDescending(c => c.CreatedAt)
                .Join(_db.Users, c => c.UserID, u => u.UserID, (c, u) => new CommentDetailDto
                {
                    CommentID = c.CommentID,
                    UserName = u.FullName,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                }).ToList();

            // lấy nhãn
            var labels = _db.TaskTagMappings
                .Where(m => m.TaskID == taskId)
                .Join(_db.TaskTags, m => m.TagID, t => t.TagID, (m, t) => t.TagName)
                .ToList();

            ViewBag.TaskLabels = labels;

            var vm = new TaskDetailViewModel
            {
                Task = task,
                Workspace = workspace,
                Assignee = assignee,
                Members = members,
                Checklists = checklists,
                Attachments = attachments,
                Comments = comments
            };

            return PartialView("_TaskDetailModalPartial", vm);
        }

        // checklist
        [HttpPost]
        [Route("ToggleChecklist")]
        public IActionResult ToggleChecklist(int checklistId)
        {
            var item = _db.TaskChecklists.FirstOrDefault(c => c.ChecklistID == checklistId);
            if (item != null)
            {
                item.IsCompleted = !item.IsCompleted;
                _db.SaveChanges();
                return Ok(new { success = true, isCompleted = item.IsCompleted });
            }
            return BadRequest();
        }

        //thêm checklist
        [HttpPost]
        [Route("AddChecklist")]
        public IActionResult AddChecklist(int taskId, string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return BadRequest();
            var newChecklist = new TaskChecklist
            {
                TaskID = taskId,
                Title = title.Trim(),
                IsCompleted = false,
                CreatedAt = DateTime.Now
            };

            _db.TaskChecklists.Add(newChecklist);
            _db.SaveChanges();
            return Ok(new { success = true, checklistId = newChecklist.ChecklistID, title = newChecklist.Title });
        }

        // add cmt
        [HttpPost]
        [Route("AddComment")]
        public IActionResult AddComment(int taskId, string content)
        {
            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(content)) return BadRequest();

            var comment = new Comment
            {
                TaskID = taskId,
                UserID = currentUserId.Value,
                Content = content.Trim(),
                CreatedAt = DateTime.Now
            };
            _db.Comments.Add(comment);
            _db.SaveChanges();

            var user = _db.Users.FirstOrDefault(u => u.UserID == currentUserId.Value);
            return Ok(new { success = true, userName = user?.FullName ?? "Bạn", content = comment.Content, createdAt = comment.CreatedAt.ToString("HH:mm dd/MM/yyyy") });
        }

        // add file đính kèm
        [HttpPost]
        [Route("UploadAttachment")]
        public async Task<IActionResult> UploadAttachment(int taskId, IFormFile file, [FromServices] IWebHostEnvironment env)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Vui lòng chọn tệp đính kèm!" });

            int? currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            try
            {
                string uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "attachments");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var attachment = new Attachment
                {
                    TaskID = taskId,
                    OriginalFileName = file.FileName,
                    StoredFileName = uniqueFileName,
                    FileURL = "/uploads/attachments/" + uniqueFileName,
                    FileSize = file.Length,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    UploadedBy = currentUserId.Value,
                    UploadedAt = DateTime.Now
                };

                _db.Attachments.Add(attachment);
                await _db.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi lưu tệp: " + ex.Message });
            }
        }

        // add nhãn
        [HttpPost]
        [Route("AddLabel")]
        public IActionResult AddLabel(int taskId, string labelName)
        {
            if (string.IsNullOrWhiteSpace(labelName))
                return BadRequest(new { success = false, message = "Tên nhãn không được trống!" });

            var task = _db.Tasks.FirstOrDefault(t => t.TaskID == taskId);
            if (task == null) return NotFound();

            string cleanLabel = labelName.Trim();

            var tag = _db.TaskTags.FirstOrDefault(t => t.WorkspaceID == task.WorkspaceID && t.TagName.ToLower() == cleanLabel.ToLower());
            if (tag == null)
            {
                tag = new TaskTag
                {
                    WorkspaceID = task.WorkspaceID,
                    TagName = cleanLabel,
                    ColorCode = "#3b82f6"
                };
                _db.TaskTags.Add(tag);
                _db.SaveChanges();
            }

            bool isMapped = _db.TaskTagMappings.Any(m => m.TaskID == taskId && m.TagID == tag.TagID);
            if (!isMapped)
            {
                _db.TaskTagMappings.Add(new TaskTagMapping
                {
                    TaskID = taskId,
                    TagID = tag.TagID
                });
                _db.SaveChanges();
            }

            return Ok(new { success = true });
        }
    }
}