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
    [Route("api/Documents/{documentId:guid}/requirements")]
    [Authorize]
    public class DocumentRequirementsController : ControllerBase
    {
        private readonly ExplainItDbContext _context;

        public DocumentRequirementsController(ExplainItDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRequirements(Guid documentId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var requirements = await _context.DocumentRequirements
                .Where(r =>
                    r.DocumentId == documentId &&
                    r.Document!.UserId == userId.Value)
                .Select(r => new DocumentRequirementResponse
                {
                    Id = r.Id,
                    DocumentId = r.DocumentId,
                    Name = r.Name,
                    Description = r.Description,
                    Completed = r.Completed
                })
                .ToListAsync();

            return Ok(requirements);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequirement(
            Guid documentId,
            CreateDocumentRequirementRequest request)
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

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { message = "Requirement name is required." });

            var requirement = new DocumentRequirement
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Name = request.Name,
                Description = request.Description,
                Completed = request.Completed
            };

            _context.DocumentRequirements.Add(requirement);
            await _context.SaveChangesAsync();

            return Ok(new DocumentRequirementResponse
            {
                Id = requirement.Id,
                DocumentId = requirement.DocumentId,
                Name = requirement.Name,
                Description = requirement.Description,
                Completed = requirement.Completed
            });
        }

        [HttpPut("{requirementId:guid}")]
        public async Task<IActionResult> UpdateRequirement(
            Guid documentId,
            Guid requirementId,
            CreateDocumentRequirementRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var requirement = await _context.DocumentRequirements
                .FirstOrDefaultAsync(r =>
                    r.Id == requirementId &&
                    r.DocumentId == documentId &&
                    r.Document!.UserId == userId.Value);

            if (requirement == null)
                return NotFound(new { message = "Requirement not found." });

            requirement.Name = request.Name;
            requirement.Description = request.Description;
            requirement.Completed = request.Completed;

            await _context.SaveChangesAsync();

            return Ok(new DocumentRequirementResponse
            {
                Id = requirement.Id,
                DocumentId = requirement.DocumentId,
                Name = requirement.Name,
                Description = requirement.Description,
                Completed = requirement.Completed
            });
        }

        [HttpDelete("{requirementId:guid}")]
        public async Task<IActionResult> DeleteRequirement(
            Guid documentId,
            Guid requirementId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var requirement = await _context.DocumentRequirements
                .FirstOrDefaultAsync(r =>
                    r.Id == requirementId &&
                    r.DocumentId == documentId &&
                    r.Document!.UserId == userId.Value);

            if (requirement == null)
                return NotFound(new
                {
                    message = "Requirement not found."
                });

            _context.DocumentRequirements.Remove(requirement);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Requirement deleted successfully."
            });
        }

        private Guid? GetUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(userId, out var parsedUserId))
                return parsedUserId;

            return null;
        }
    }
}