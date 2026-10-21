using ExplainIt.API.Data;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;
namespace ExplainIt.API.Controllers.Advanced;
[ApiController][Route("api/shared")]
public class SharedController:ControllerBase{
 private readonly ExplainItDbContext _db;public SharedController(ExplainItDbContext db)=>_db=db;
 [HttpGet("{token}")] public async Task<IActionResult> Get(string token){var s=await _db.DocumentShares.Include(x=>x.Document).ThenInclude(d=>d!.Category).Include(x=>x.Document).ThenInclude(d=>d!.Actions).Include(x=>x.Document).ThenInclude(d=>d!.Requirements).Include(x=>x.Document).ThenInclude(d=>d!.Deadlines).Include(x=>x.Document).ThenInclude(d=>d!.Terms).FirstOrDefaultAsync(x=>x.Token==token&&!x.Revoked&&(x.ExpiresAt==null||x.ExpiresAt>DateTime.UtcNow));if(s?.Document==null)return NotFound(new{message="This share link is invalid or expired."});var d=s.Document;return Ok(new{d.Title,category=d.Category?.Name,d.SimpleSummary,d.WhatIsIt,d.WhyReceived,d.WhatToDo,d.Consequences,actions=d.Actions.Select(a=>a.Title),requirements=d.Requirements.Select(r=>r.Name),deadlines=d.Deadlines.Select(x=>new{x.Title,x.Deadline}),terms=d.Terms.Select(t=>new{t.Term,t.SimpleExplanation})});}
}
