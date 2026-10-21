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
    [Route("api/Documents/{documentId:guid}/actions")]
    [Authorize]
    public class DocumentActionsController : ControllerBase
    {
        private readonly ExplainItDbContext _context;

        public DocumentActionsController(ExplainItDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAction(
            Guid documentId,
            CreateDocumentActionRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var documentExists = await _context.Documents
                .AnyAsync(d =>
                    d.Id == documentId &&
                    d.UserId == userId.Value);

            if (!documentExists)
            {
                return NotFound(new
                {
                    message = "Document not found."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    message = "Action title is required."
                });
            }

            var action = new DocumentAction
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Title = request.Title,
                Description = request.Description,
                Status = request.Status,
                Deadline = request.Deadline
            };

            _context.DocumentActions.Add(action);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetAction),
                new
                {
                    documentId,
                    actionId = action.Id
                },
                new DocumentActionResponse
                {
                    Id = action.Id,
                    DocumentId = action.DocumentId,
                    Title = action.Title,
                    Description = action.Description,
                    Status = action.Status,
                    Deadline = action.Deadline
                });
        }

        [HttpGet]
        public async Task<IActionResult> GetActions(Guid documentId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var documentExists = await _context.Documents
                .AnyAsync(d =>
                    d.Id == documentId &&
                    d.UserId == userId.Value);

            if (!documentExists)
            {
                return NotFound(new
                {
                    message = "Document not found."
                });
            }

            var actions = await _context.DocumentActions
                .Where(a => a.DocumentId == documentId)
                .OrderBy(a => a.Deadline)
                .Select(a => new DocumentActionResponse
                {
                    Id = a.Id,
                    DocumentId = a.DocumentId,
                    Title = a.Title,
                    Description = a.Description,
                    Status = a.Status,
                    Deadline = a.Deadline
                })
                .ToListAsync();

            return Ok(actions);
        }

        [HttpGet("{actionId:guid}")]
        public async Task<IActionResult> GetAction(
            Guid documentId,
            Guid actionId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var action = await _context.DocumentActions
                .Where(a =>
                    a.Id == actionId &&
                    a.DocumentId == documentId &&
                    a.Document!.UserId == userId.Value)
                .Select(a => new DocumentActionResponse
                {
                    Id = a.Id,
                    DocumentId = a.DocumentId,
                    Title = a.Title,
                    Description = a.Description,
                    Status = a.Status,
                    Deadline = a.Deadline
                })
                .FirstOrDefaultAsync();

            if (action == null)
            {
                return NotFound(new
                {
                    message = "Action not found."
                });
            }

            return Ok(action);
        }

        [HttpPut("{actionId:guid}")]
        public async Task<IActionResult> UpdateAction(
            Guid documentId,
            Guid actionId,
            CreateDocumentActionRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var action = await _context.DocumentActions
                .FirstOrDefaultAsync(a =>
                    a.Id == actionId &&
                    a.DocumentId == documentId &&
                    a.Document!.UserId == userId.Value);

            if (action == null)
            {
                return NotFound(new
                {
                    message = "Action not found."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest(new
                {
                    message = "Action title is required."
                });
            }

            action.Title = request.Title;
            action.Description = request.Description;
            action.Status = request.Status;
            action.Deadline = request.Deadline;

            await _context.SaveChangesAsync();

            return Ok(new DocumentActionResponse
            {
                Id = action.Id,
                DocumentId = action.DocumentId,
                Title = action.Title,
                Description = action.Description,
                Status = action.Status,
                Deadline = action.Deadline
            });
        }

        [HttpDelete("{actionId:guid}")]
        public async Task<IActionResult> DeleteAction(
    Guid documentId,
    Guid actionId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var action = await _context.DocumentActions
                .FirstOrDefaultAsync(a =>
                    a.Id == actionId &&
                    a.DocumentId == documentId &&
                    a.Document!.UserId == userId.Value);

            if (action == null)
            {
                return NotFound(new
                {
                    message = "Action not found."
                });
            }

            _context.DocumentActions.Remove(action);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Action deleted successfully."
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