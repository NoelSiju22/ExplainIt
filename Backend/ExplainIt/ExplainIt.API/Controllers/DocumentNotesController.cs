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
    [Route("api/Documents/{documentId:guid}/notes")]
    [Authorize]
    public class DocumentNotesController : ControllerBase
    {
        private readonly ExplainItDbContext _context;

        public DocumentNotesController(ExplainItDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotes(Guid documentId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var notes = await _context.DocumentNotes
                .Where(n =>
                    n.DocumentId == documentId &&
                    n.Document!.UserId == userId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new DocumentNoteResponse
                {
                    Id = n.Id,
                    DocumentId = n.DocumentId,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt
                })
                .ToListAsync();

            return Ok(notes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNote(
            Guid documentId,
            CreateDocumentNoteRequest request)
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

            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new
                {
                    message = "Note content is required."
                });

            var note = new DocumentNote
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            _context.DocumentNotes.Add(note);

            await _context.SaveChangesAsync();

            return Ok(new DocumentNoteResponse
            {
                Id = note.Id,
                DocumentId = note.DocumentId,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            });
        }

        [HttpPut("{noteId:guid}")]
        public async Task<IActionResult> UpdateNote(
            Guid documentId,
            Guid noteId,
            CreateDocumentNoteRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var note = await _context.DocumentNotes
                .FirstOrDefaultAsync(n =>
                    n.Id == noteId &&
                    n.DocumentId == documentId &&
                    n.Document!.UserId == userId.Value);

            if (note == null)
                return NotFound(new
                {
                    message = "Note not found."
                });

            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new
                {
                    message = "Note content is required."
                });

            note.Content = request.Content;
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new DocumentNoteResponse
            {
                Id = note.Id,
                DocumentId = note.DocumentId,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            });
        }

        [HttpDelete("{noteId:guid}")]
        public async Task<IActionResult> DeleteNote(
            Guid documentId,
            Guid noteId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var note = await _context.DocumentNotes
                .FirstOrDefaultAsync(n =>
                    n.Id == noteId &&
                    n.DocumentId == documentId &&
                    n.Document!.UserId == userId.Value);

            if (note == null)
                return NotFound(new
                {
                    message = "Note not found."
                });

            _context.DocumentNotes.Remove(note);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Note deleted successfully."
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