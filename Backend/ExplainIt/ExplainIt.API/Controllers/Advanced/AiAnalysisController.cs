using ExplainIt.API.Data;
using ExplainIt.API.Models;
using ExplainIt.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using UglyToad.PdfPig;

namespace ExplainIt.API.Controllers.Advanced;

[ApiController]
[Route("api/documents/{documentId:guid}/ai")]
[Authorize]
public class AiAnalysisController : ControllerBase
{
    private readonly ExplainItDbContext _db;
    private readonly OfflineNlpService _nlp;

    public AiAnalysisController(ExplainItDbContext db, OfflineNlpService nlp)
    {
        _db = db;
        _nlp = nlp;
    }

    private Guid? UserId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(Guid documentId, CancellationToken ct)
    {
        var userId = UserId();
        if (userId == null) return Unauthorized();

        var d = await _db.Documents
            .Include(x => x.Actions)
            .Include(x => x.Requirements)
            .Include(x => x.Deadlines)
            .Include(x => x.Terms)
            .FirstOrDefaultAsync(x => x.Id == documentId && x.UserId == userId && x.DeletedAt == null, ct);

        if (d == null) return NotFound(new { message = "Document not found." });
        if (string.IsNullOrWhiteSpace(d.FilePath))
            return BadRequest(new { message = "Upload a document before asking ExplainIt to analyze it." });

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Uploads", d.FilePath);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "The uploaded document could not be found." });

        string sourceText;
        try
        {
            sourceText = ExtractPdfText(filePath);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"ExplainIt could not read this PDF: {ex.Message}" });
        }

        if (string.IsNullOrWhiteSpace(sourceText))
            return BadRequest(new { message = "This PDF does not contain readable text. Scanned-image PDFs need local OCR support before ExplainIt can analyze them." });

        d.SourceText = sourceText.Length > 120000 ? sourceText[..120000] : sourceText;

        JsonDocument analysis;
        try
        {
            analysis = await _nlp.AnalyzeAsync(d.SourceText, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return StatusCode(503, new
            {
                message = "The local Python NLP service is unavailable or returned an invalid response. Start it from the ExplainIt/NLP folder and try again.",
                detail = ex.Message
            });
        }

        using (analysis)
        {
            try
            {
                var root = analysis.RootElement;
                d.Title = Get(root, "documentTitle") ?? d.Title;
                d.Description = Get(root, "documentType");
                d.SimpleSummary = Get(root, "summary");
                d.WhatIsIt = Get(root, "whatIsIt");
                d.WhyReceived = Get(root, "whyReceived");
                d.WhatToDo = Get(root, "whatToDo");
                d.Consequences = Get(root, "consequences");
                d.Status = Get(root, "status") ?? "Needs attention";

                var dateText = Get(root, "documentDate");
                if (DateTime.TryParse(dateText, out var docDate))
                    d.DocumentDate = DateTime.SpecifyKind(docDate.Date, DateTimeKind.Utc);

                var category = Get(root, "category");
                if (!string.IsNullOrWhiteSpace(category))
                {
                    var categoryEntity = await _db.DocumentCategories
                        .FirstOrDefaultAsync(c => c.Name.ToLower() == category.ToLower(), ct);
                    if (categoryEntity != null) d.CategoryId = categoryEntity.Id;
                }

                // Replace previous generated items on every re-analysis so old items do not linger.
                _db.DocumentActions.RemoveRange(d.Actions);
                _db.DocumentRequirements.RemoveRange(d.Requirements);
                _db.DocumentDeadlines.RemoveRange(d.Deadlines);
                _db.DocumentTerms.RemoveRange(d.Terms);

                if (root.TryGetProperty("actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in actions.EnumerateArray())
                    {
                        var title = Get(item, "title");
                        if (string.IsNullOrWhiteSpace(title)) continue;
                        _db.DocumentActions.Add(new DocumentAction
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = d.Id,
                            Title = title,
                            Description = Get(item, "description"),
                            Status = "Pending",
                            Deadline = ParseDate(Get(item, "deadline"))
                        });
                    }
                }

                if (root.TryGetProperty("requirements", out var requirements) && requirements.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in requirements.EnumerateArray())
                    {
                        var name = Get(item, "name");
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        _db.DocumentRequirements.Add(new DocumentRequirement
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = d.Id,
                            Name = name,
                            Description = Get(item, "description"),
                            Completed = false
                        });
                    }
                }

                if (root.TryGetProperty("deadlines", out var deadlines) && deadlines.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in deadlines.EnumerateArray())
                    {
                        var title = Get(item, "title");
                        var date = ParseDate(Get(item, "date"));
                        if (string.IsNullOrWhiteSpace(title) || date == null) continue;
                        _db.DocumentDeadlines.Add(new DocumentDeadline
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = d.Id,
                            Title = title,
                            Deadline = date.Value,
                            IsCompleted = false
                        });
                    }
                }

                if (root.TryGetProperty("terms", out var terms) && terms.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in terms.EnumerateArray())
                    {
                        var term = Get(item, "term");
                        var explanation = Get(item, "explanation");
                        if (string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(explanation)) continue;
                        _db.DocumentTerms.Add(new DocumentTerm
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = d.Id,
                            Term = term,
                            SimpleExplanation = explanation
                        });
                    }
                }

                _db.DocumentEvents.Add(new DocumentEvent
                {
                    Id = Guid.NewGuid(),
                    DocumentId = d.Id,
                    Type = "offline-nlp-analysis",
                    Description = "ExplainIt analyzed the uploaded document using its local Python NLP service.",
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync(ct);
                return Ok(new
                {
                    message = "ExplainIt analyzed the document using the local offline NLP engine.",
                    documentId = d.Id,
                    title = d.Title,
                    status = d.Status,
                    analysisMethod = Get(root, "analysisMethod"),
                    classificationLabel = root.TryGetProperty("classification", out var classification) ? Get(classification, "label") : null,
                    classificationConfidencePercent = root.TryGetProperty("classification", out var confidenceObject)
                        && confidenceObject.TryGetProperty("confidencePercent", out var confidenceValue)
                        && confidenceValue.TryGetDouble(out var confidenceNumber)
                            ? confidenceNumber
                            : (double?)null
                });
            }
            catch (JsonException)
            {
                return StatusCode(502, new { message = "ExplainIt could not parse the local NLP result. Please try Analyze again." });
            }
        }
    }

    private static string ExtractPdfText(string filePath)
    {
        using var pdf = PdfDocument.Open(filePath);
        return string.Join("\n\n", pdf.GetPages().Select(p => p.Text?.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)));
    }

    private static string? Get(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateTime? ParseDate(string? value)
    {
        if (!DateTime.TryParse(value, out var date)) return null;
        return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }
}
