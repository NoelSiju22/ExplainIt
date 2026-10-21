using ExplainIt.API.Data;
using ExplainIt.API.DTOs;
using ExplainIt.API.Models;
using ExplainIt.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExplainIt.API.Controllers.Advanced;
[ApiController]
[Route("api/documents/{documentId:guid}/questions")]
[Authorize]
public class QuestionsController : ControllerBase
{
    private readonly ExplainItDbContext _db; private readonly ExplainItAiService _ai;
    public QuestionsController(ExplainItDbContext db,ExplainItAiService ai){_db=db;_ai=ai;}
    private Guid? U()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
    [HttpGet] public async Task<IActionResult> Get(Guid documentId){var u=U();if(u==null)return Unauthorized();var ok=await _db.Documents.AnyAsync(d=>d.Id==documentId&&d.UserId==u);if(!ok)return NotFound();return Ok(await _db.DocumentQuestions.Where(q=>q.DocumentId==documentId).OrderByDescending(q=>q.CreatedAt).Select(q=>new{q.Id,q.Question,q.Answer,q.Mode,q.CreatedAt}).ToListAsync());}
    [HttpPost] public async Task<IActionResult> Ask(Guid documentId,QuestionRequest r){var u=U();if(u==null)return Unauthorized();var d=await _db.Documents.Include(x=>x.Terms).Include(x=>x.Actions).Include(x=>x.Requirements).Include(x=>x.Deadlines).FirstOrDefaultAsync(x=>x.Id==documentId&&x.UserId==u);if(d==null)return NotFound();if(string.IsNullOrWhiteSpace(r.Question))return BadRequest(new{message="Question is required."});var context=$"Title: {d.Title}\nDescription: {d.Description}\nSummary: {d.SimpleSummary}\nWhat is it: {d.WhatIsIt}\nWhy received: {d.WhyReceived}\nWhat to do: {d.WhatToDo}\nConsequences: {d.Consequences}\nActions: {string.Join("; ",d.Actions.Select(x=>x.Title))}\nRequirements: {string.Join("; ",d.Requirements.Select(x=>x.Name))}\nDeadlines: {string.Join("; ",d.Deadlines.Select(x=>$"{x.Title} {x.Deadline:d}"))}\nTerms: {string.Join("; ",d.Terms.Select(x=>$"{x.Term}: {x.SimpleExplanation}"))}";var answer=await _ai.AskAsync(context,r.Question,r.Mode);var q=new DocumentQuestion{Id=Guid.NewGuid(),DocumentId=d.Id,Question=r.Question,Answer=answer,Mode=r.Mode};_db.DocumentQuestions.Add(q);await _db.SaveChangesAsync();return Ok(new{q.Id,q.Question,q.Answer,q.Mode,q.CreatedAt});}
}
