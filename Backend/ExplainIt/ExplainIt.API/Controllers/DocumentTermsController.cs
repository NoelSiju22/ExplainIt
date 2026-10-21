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
    [Route("api/Documents/{documentId:guid}/terms")]
    [Authorize]
    public class DocumentTermsController : ControllerBase
    {
        private readonly ExplainItDbContext _context;

        public DocumentTermsController(ExplainItDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetTerms(Guid documentId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var terms = await _context.DocumentTerms
                .Where(t =>
                    t.DocumentId == documentId &&
                    t.Document!.UserId == userId.Value)
                .Select(t => new DocumentTermResponse
                {
                    Id = t.Id,
                    DocumentId = t.DocumentId,
                    Term = t.Term,
                    SimpleExplanation = t.SimpleExplanation
                })
                .ToListAsync();

            return Ok(terms);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTerm(
            Guid documentId,
            CreateDocumentTermRequest request)
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

            if (string.IsNullOrWhiteSpace(request.Term) ||
                string.IsNullOrWhiteSpace(request.SimpleExplanation))
            {
                return BadRequest(new
                {
                    message = "Term and explanation are required."
                });
            }

            var term = new DocumentTerm
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                Term = request.Term,
                SimpleExplanation = request.SimpleExplanation
            };

            _context.DocumentTerms.Add(term);

            await _context.SaveChangesAsync();

            return Ok(new DocumentTermResponse
            {
                Id = term.Id,
                DocumentId = term.DocumentId,
                Term = term.Term,
                SimpleExplanation = term.SimpleExplanation
            });
        }

        [HttpPut("{termId:guid}")]
        public async Task<IActionResult> UpdateTerm(
            Guid documentId,
            Guid termId,
            CreateDocumentTermRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var term = await _context.DocumentTerms
                .FirstOrDefaultAsync(t =>
                    t.Id == termId &&
                    t.DocumentId == documentId &&
                    t.Document!.UserId == userId.Value);

            if (term == null)
                return NotFound(new { message = "Term not found." });

            term.Term = request.Term;
            term.SimpleExplanation = request.SimpleExplanation;

            await _context.SaveChangesAsync();

            return Ok(new DocumentTermResponse
            {
                Id = term.Id,
                DocumentId = term.DocumentId,
                Term = term.Term,
                SimpleExplanation = term.SimpleExplanation
            });
        }

        [HttpDelete("{termId:guid}")]
        public async Task<IActionResult> DeleteTerm(
            Guid documentId,
            Guid termId)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var term = await _context.DocumentTerms
                .FirstOrDefaultAsync(t =>
                    t.Id == termId &&
                    t.DocumentId == documentId &&
                    t.Document!.UserId == userId.Value);

            if (term == null)
                return NotFound(new { message = "Term not found." });

            _context.DocumentTerms.Remove(term);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Term deleted successfully."
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