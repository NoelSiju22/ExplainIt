using ExplainIt.API.Data;
using ExplainIt.API.DTOs;
using ExplainIt.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExplainIt.API.Controllers
{
    [ApiController]
    [Route("api/Documents/{documentId:guid}/deadlines")]
    [Authorize]
    public class DocumentDeadlinesController : ControllerBase
    {
        private readonly ExplainItDbContext _context;

        public DocumentDeadlinesController(ExplainItDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDeadlines(Guid documentId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var deadlines = await _context.DocumentDeadlines
                .Where(d =>
                    d.DocumentId == documentId &&
                    d.Document!.UserId == userId.Value)
                .OrderBy(d => d.Deadline)
                .Select(d => new DocumentDeadlineResponse
                {
                    Id = d.Id,
                    DocumentId = d.DocumentId,
                    Title = d.Title,
                    Deadline = d.Deadline,
                    IsCompleted = d.IsCompleted
                })
                .ToListAsync();

            return Ok(deadlines);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDeadline(
            Guid documentId,
            CreateDocumentDeadlineRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var documentExists = await _context.Documents
                .AnyAsync(d =>
                    d.Id == documentId &&
                    d.UserId == userId.Value);

            if (!documentExists)
                return NotFound(new { message = "Document not found." });

            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest(new { message = "Deadline title is required." });

            var deadline = new DocumentDeadline
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Title = request.Title,
                Deadline = request.Deadline,
                IsCompleted = request.IsCompleted
            };

            _context.DocumentDeadlines.Add(deadline);

            await _context.SaveChangesAsync();

            return Ok(new DocumentDeadlineResponse
            {
                Id = deadline.Id,
                DocumentId = deadline.DocumentId,
                Title = deadline.Title,
                Deadline = deadline.Deadline,
                IsCompleted = deadline.IsCompleted
            });
        }

        [HttpPut("{deadlineId:guid}")]
        public async Task<IActionResult> UpdateDeadline(
            Guid documentId,
            Guid deadlineId,
            CreateDocumentDeadlineRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var deadline = await _context.DocumentDeadlines
                .FirstOrDefaultAsync(d =>
                    d.Id == deadlineId &&
                    d.DocumentId == documentId &&
                    d.Document!.UserId == userId.Value);

            if (deadline == null)
                return NotFound(new { message = "Deadline not found." });

            deadline.Title = request.Title;
            deadline.Deadline = request.Deadline;
            deadline.IsCompleted = request.IsCompleted;

            await _context.SaveChangesAsync();

            return Ok(new DocumentDeadlineResponse
            {
                Id = deadline.Id,
                DocumentId = deadline.DocumentId,
                Title = deadline.Title,
                Deadline = deadline.Deadline,
                IsCompleted = deadline.IsCompleted
            });
        }

        [HttpDelete("{deadlineId:guid}")]
        public async Task<IActionResult> DeleteDeadline(
    Guid documentId,
    Guid deadlineId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var deadline = await _context.DocumentDeadlines
                .FirstOrDefaultAsync(d =>
                    d.Id == deadlineId &&
                    d.DocumentId == documentId &&
                    d.Document!.UserId == userId.Value);

            if (deadline == null)
                return NotFound(new
                {
                    message = "Deadline not found."
                });

            _context.DocumentDeadlines.Remove(deadline);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Deadline deleted successfully."
            });
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