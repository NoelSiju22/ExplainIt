using ExplainIt.API.Data;
using ExplainIt.API.DTOs;
using ExplainIt.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace ExplainIt.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly ExplainItDbContext _context;

        public DocumentsController(ExplainItDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDocument(
            CreateDocumentRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            // The user should not have to classify or describe the document.
            // ExplainIt will infer the title, type, category and meaning from the uploaded PDF.
            var categoryId = request.CategoryId;
            if (categoryId <= 0)
            {
                var fallback = await _context.DocumentCategories
                    .OrderBy(c => c.Id)
                    .FirstOrDefaultAsync();

                if (fallback == null)
                    return BadRequest(new { message = "No document categories are configured." });

                categoryId = fallback.Id;
            }
            else if (!await _context.DocumentCategories.AnyAsync(c => c.Id == categoryId))
            {
                return BadRequest(new { message = "Invalid document category." });
            }

            var document = new Document
            {
                Id = Guid.NewGuid(),
                UserId = userId.Value,
                CategoryId = categoryId,
                Title = string.IsNullOrWhiteSpace(request.Title) ? "New document" : request.Title,
                FilePath = request.FilePath ?? string.Empty,
                Description = request.Description,
                IsImportant = request.IsImportant,
                DocumentDate = request.DocumentDate,
                UploadedAt = DateTime.UtcNow
            };

            _context.Documents.Add(document);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetDocument),
                new { id = document.Id },
                document);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyDocuments(
    string? search,
    int? categoryId,
    bool? important)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var query = _context.Documents
                .Where(d => d.UserId == userId.Value && d.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(d =>
                    d.Title.Contains(search) ||
                    (d.Description != null && d.Description.Contains(search)) ||
                    d.Tags.Any(t => t.Name.Contains(search)) ||
                    d.Terms.Any(t => t.Term.Contains(search) || t.SimpleExplanation.Contains(search)) ||
                    d.Actions.Any(a => a.Title.Contains(search) || (a.Description != null && a.Description.Contains(search))) ||
                    d.Requirements.Any(r => r.Name.Contains(search) || (r.Description != null && r.Description.Contains(search))));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(d =>
                    d.CategoryId == categoryId.Value);
            }

            if (important.HasValue)
            {
                query = query.Where(d =>
                    d.IsImportant == important.Value);
            }

            var documents = await query
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new DocumentListResponse
                {
                    Id = d.Id,
                    CategoryId = d.CategoryId,
                    CategoryName = d.Category != null
                        ? d.Category.Name
                        : string.Empty,
                    Title = d.Title,
                    FilePath = d.FilePath,
                    Description = d.Description,
                    UploadedAt = d.UploadedAt,
                    IsImportant = d.IsImportant,
                    DocumentDate = d.DocumentDate,
                    Status = d.Status
                })
                .ToListAsync();

            return Ok(documents);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDocument(Guid id)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var document = await _context.Documents
                .Where(d =>
                    d.Id == id &&
                    d.UserId == userId.Value &&
                    d.DeletedAt == null)
                .Select(d => new DocumentDetailResponse
                {
                    Id = d.Id,
                    CategoryId = d.CategoryId,
                    CategoryName = d.Category != null
                        ? d.Category.Name
                        : string.Empty,
                    Title = d.Title,
                    FilePath = d.FilePath,
                    Description = d.Description,
                    UploadedAt = d.UploadedAt,
                    IsImportant = d.IsImportant,
                    DocumentDate = d.DocumentDate,
                    Status = d.Status,
                    SimpleSummary = d.SimpleSummary,
                    WhatIsIt = d.WhatIsIt,
                    WhyReceived = d.WhyReceived,
                    WhatToDo = d.WhatToDo,
                    Consequences = d.Consequences,
                    LastViewedAt = d.LastViewedAt,

                    Actions = d.Actions
                        .Select(a => new DocumentActionResponse
                        {
                            Id = a.Id,
                            Title = a.Title,
                            Description = a.Description,
                            Status = a.Status,
                            Deadline = a.Deadline
                        })
                        .ToList(),

                    Deadlines = d.Deadlines
                        .Select(deadline => new DocumentDeadlineResponse
                        {
                            Id = deadline.Id,
                            Title = deadline.Title,
                            Deadline = deadline.Deadline,
                            IsCompleted = deadline.IsCompleted
                        })
                        .ToList(),

                    Requirements = d.Requirements
                        .Select(r => new DocumentRequirementResponse
                        {
                            Id = r.Id,
                            Name = r.Name,
                            Description = r.Description,
                            Completed = r.Completed
                        })
                        .ToList(),

                    Terms = d.Terms
                        .Select(t => new DocumentTermResponse
                        {
                            Id = t.Id,
                            Term = t.Term,
                            SimpleExplanation = t.SimpleExplanation
                        })
                        .ToList(),

                    Notes = d.Notes
                        .Select(n => new DocumentNoteResponse
                        {
                            Id = n.Id,
                            Content = n.Content,
                            CreatedAt = n.CreatedAt,
                            UpdatedAt = n.UpdatedAt
                        })
                        .ToList(),

                    Versions = d.Versions.OrderByDescending(v => v.VersionNumber).Select(v => new DocumentVersionResponse { Id=v.Id, DocumentId=v.DocumentId, VersionNumber=v.VersionNumber, Title=v.Title, Description=v.Description, FilePath=v.FilePath, ChangeSummary=v.ChangeSummary, CreatedAt=v.CreatedAt }).ToList(),
                    Tags = d.Tags.OrderBy(t => t.Name).Select(t => new DocumentTagResponse { Id=t.Id, Name=t.Name }).ToList(),
                    Events = d.Events.OrderByDescending(e => e.CreatedAt).Select(e => new DocumentEventResponse { Id=e.Id, Type=e.Type, Description=e.Description, CreatedAt=e.CreatedAt }).ToList(),
                    Reminders = d.Reminders.OrderBy(r => r.RemindAt).Select(r => new DocumentReminderResponse { Id=r.Id, DocumentId=r.DocumentId, Title=r.Title, RemindAt=r.RemindAt, IsRead=r.IsRead }).ToList(),
                    Questions = d.Questions.OrderByDescending(q => q.CreatedAt).Select(q => new DocumentQuestionResponse { Id=q.Id, Question=q.Question, Answer=q.Answer, Mode=q.Mode, CreatedAt=q.CreatedAt }).ToList()
                })
                .FirstOrDefaultAsync();

            if (document == null)
            {
                return NotFound(new
                {
                    message = "Document not found."
                });
            }

            return Ok(document);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateDocument(
            Guid id,
            CreateDocumentRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var document = await _context.Documents
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.UserId == userId.Value);

            if (document == null)
            {
                return NotFound(new
                {
                    message = "Document not found."
                });
            }

            var categoryExists = await _context.DocumentCategories
                .AnyAsync(c => c.Id == request.CategoryId);

            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = "Invalid document category."
                });
            }

            document.CategoryId = request.CategoryId;
            document.Title = request.Title;
            document.FilePath = request.FilePath ?? document.FilePath;
            document.Description = request.Description;
            document.IsImportant = request.IsImportant;
            document.DocumentDate = request.DocumentDate;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Document updated successfully.",
                documentId = document.Id
            });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteDocument(Guid id)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var document = await _context.Documents
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.UserId == userId.Value);

            if (document == null)
            {
                return NotFound(new
                {
                    message = "Document not found."
                });
            }

            document.DeletedAt = DateTime.UtcNow;
            _context.DocumentEvents.Add(new DocumentEvent
            { Id = Guid.NewGuid(), DocumentId = document.Id, Type = "trash", Description = "Document moved to Trash." });
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Document deleted successfully."
            });
        }

        [HttpPost("{id:guid}/file")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadDocumentFile(
            Guid id,
            IFormFile file)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please select a file."
                });
            }

            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new
                {
                    message = "File size cannot exceed 10 MB."
                });
            }

            var extension = Path.GetExtension(file.FileName);

            if (!string.Equals(
                extension,
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Only PDF files are allowed."
                });
            }

            var document = await _context.Documents
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.UserId == userId.Value);

            if (document == null)
            {
                return NotFound(new
                {
                    message = "Document not found."
                });
            }

            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads");

            Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrWhiteSpace(document.FilePath))
            {
                var oldFilePath = Path.Combine(
                    uploadsFolder,
                    document.FilePath);

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            var uniqueFileName =
                $"{Guid.NewGuid()}.pdf";

            var filePath = Path.Combine(
                uploadsFolder,
                uniqueFileName);

            await using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            document.FilePath = uniqueFileName;
            var nextVersion = (await _context.DocumentVersions.Where(v => v.DocumentId == document.Id).MaxAsync(v => (int?)v.VersionNumber) ?? 0) + 1;
            _context.DocumentVersions.Add(new DocumentVersion { Id = Guid.NewGuid(), DocumentId = document.Id, VersionNumber = nextVersion, Title = document.Title, Description = document.Description, FilePath = uniqueFileName, ChangeSummary = nextVersion == 1 ? "Initial document upload." : "New PDF uploaded." });
            _context.DocumentEvents.Add(new DocumentEvent { Id = Guid.NewGuid(), DocumentId = document.Id, Type = "upload", Description = nextVersion == 1 ? "PDF uploaded." : $"PDF version {nextVersion} uploaded." });

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "PDF uploaded successfully.",
                documentId = document.Id,
                fileName = uniqueFileName
            });
        }

        [HttpGet("{id:guid}/file")]
        public async Task<IActionResult> DownloadDocumentFile(Guid id)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var document = await _context.Documents
                .FirstOrDefaultAsync(d =>
                    d.Id == id &&
                    d.UserId == userId.Value);

            if (document == null)
            {
                return NotFound(new
                {
                    message = "Document not found."
                });
            }

            if (string.IsNullOrWhiteSpace(document.FilePath))
            {
                return NotFound(new
                {
                    message = "No file has been uploaded for this document."
                });
            }

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Uploads",
                document.FilePath);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new
                {
                    message = "Stored file could not be found."
                });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            return File(
                fileBytes,
                "application/pdf",
                document.Title + ".pdf");
        }

        private Guid? GetUserId()
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userId, out var parsedUserId))
                return parsedUserId;

            return null;
        }
    }
}