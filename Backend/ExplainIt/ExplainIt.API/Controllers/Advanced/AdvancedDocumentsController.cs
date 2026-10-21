using ExplainIt.API.Data;
using ExplainIt.API.DTOs;
using ExplainIt.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExplainIt.API.Controllers.Advanced;

[ApiController]
[Route("api/documents/{documentId:guid}/advanced")]
[Authorize]
public class AdvancedDocumentsController : ControllerBase
{
    private readonly ExplainItDbContext _db;
    public AdvancedDocumentsController(ExplainItDbContext db) => _db = db;
    private Guid? UserId() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    private async Task<Document?> Owned(Guid id) { var uid=UserId(); return uid==null?null:await _db.Documents.FirstOrDefaultAsync(d=>d.Id==id&&d.UserId==uid.Value); }

    [HttpPut("intelligence")]
    public async Task<IActionResult> Intelligence(Guid documentId, IntelligenceRequest r)
    {
        var d=await Owned(documentId); if(d==null)return NotFound();
        d.SimpleSummary=r.SimpleSummary; d.WhatIsIt=r.WhatIsIt; d.WhyReceived=r.WhyReceived; d.WhatToDo=r.WhatToDo; d.Consequences=r.Consequences;
        if(!string.IsNullOrWhiteSpace(r.Status)) d.Status=r.Status;
        _db.DocumentEvents.Add(new DocumentEvent{Id=Guid.NewGuid(),DocumentId=d.Id,Type="explanation",Description="Document explanation updated."});
        await _db.SaveChangesAsync(); return Ok(new{message="Explanation saved."});
    }

    [HttpGet("tags")]
    public async Task<IActionResult> Tags(Guid documentId){var d=await Owned(documentId);if(d==null)return NotFound();return Ok(await _db.DocumentTags.Where(t=>t.DocumentId==d.Id).OrderBy(t=>t.Name).Select(t=>new{t.Id,t.Name}).ToListAsync());}
    [HttpPost("tags")]
    public async Task<IActionResult> AddTag(Guid documentId,TagRequest r){var d=await Owned(documentId);if(d==null)return NotFound();var name=r.Name.Trim();if(name=="")return BadRequest(new{message="Tag name is required."});if(await _db.DocumentTags.AnyAsync(t=>t.DocumentId==d.Id&&t.Name==name))return Ok(new{message="Tag already exists."});var t=new DocumentTag{Id=Guid.NewGuid(),DocumentId=d.Id,Name=name};_db.DocumentTags.Add(t);_db.DocumentEvents.Add(new DocumentEvent{Id=Guid.NewGuid(),DocumentId=d.Id,Type="tag",Description=$"Tag added: {name}"});await _db.SaveChangesAsync();return Ok(new{t.Id,t.Name});}
    [HttpDelete("tags/{tagId:guid}")]
    public async Task<IActionResult> DeleteTag(Guid documentId,Guid tagId){var d=await Owned(documentId);if(d==null)return NotFound();var t=await _db.DocumentTags.FirstOrDefaultAsync(x=>x.Id==tagId&&x.DocumentId==d.Id);if(t==null)return NotFound();_db.DocumentTags.Remove(t);await _db.SaveChangesAsync();return Ok();}

    [HttpGet("versions")]
    public async Task<IActionResult> Versions(Guid documentId){var d=await Owned(documentId);if(d==null)return NotFound();return Ok(await _db.DocumentVersions.Where(v=>v.DocumentId==d.Id).OrderByDescending(v=>v.VersionNumber).Select(v=>new{v.Id,v.VersionNumber,v.Title,v.Description,v.FilePath,v.ChangeSummary,v.CreatedAt}).ToListAsync());}
    [HttpPost("versions")]
    public async Task<IActionResult> Snapshot(Guid documentId,VersionRequest r){var d=await Owned(documentId);if(d==null)return NotFound();var next=(await _db.DocumentVersions.Where(v=>v.DocumentId==d.Id).MaxAsync(v=>(int?)v.VersionNumber)??0)+1;var v=new DocumentVersion{Id=Guid.NewGuid(),DocumentId=d.Id,VersionNumber=next,Title=d.Title,Description=d.Description,FilePath=d.FilePath,ChangeSummary=r.ChangeSummary,CreatedAt=DateTime.UtcNow};_db.DocumentVersions.Add(v);_db.DocumentEvents.Add(new DocumentEvent{Id=Guid.NewGuid(),DocumentId=d.Id,Type="version",Description=$"Version {next} created."});await _db.SaveChangesAsync();return Ok(new{v.Id,v.VersionNumber});}
    [HttpGet("compare/{a:int}/{b:int}")]
    public async Task<IActionResult> Compare(Guid documentId,int a,int b){var d=await Owned(documentId);if(d==null)return NotFound();var vs=await _db.DocumentVersions.Where(v=>v.DocumentId==d.Id&&(v.VersionNumber==a||v.VersionNumber==b)).OrderBy(v=>v.VersionNumber).ToListAsync();return Ok(vs.Select(v=>new{v.VersionNumber,v.Title,v.Description,v.ChangeSummary,v.CreatedAt}));}

    [HttpGet("events")]
    public async Task<IActionResult> Events(Guid documentId){var d=await Owned(documentId);if(d==null)return NotFound();return Ok(await _db.DocumentEvents.Where(e=>e.DocumentId==d.Id).OrderByDescending(e=>e.CreatedAt).Select(e=>new{e.Id,e.Type,e.Description,e.CreatedAt}).ToListAsync());}

    [HttpGet("reminders")]
    public async Task<IActionResult> Reminders(Guid documentId){var d=await Owned(documentId);if(d==null)return NotFound();return Ok(await _db.DocumentReminders.Where(r=>r.DocumentId==d.Id).OrderBy(r=>r.RemindAt).Select(r=>new{r.Id,r.Title,r.RemindAt,r.IsRead,r.DocumentId}).ToListAsync());}
    [HttpPost("reminders")]
    public async Task<IActionResult> AddReminder(Guid documentId,ReminderRequest r){var d=await Owned(documentId);if(d==null)return NotFound();var x=new DocumentReminder{Id=Guid.NewGuid(),DocumentId=d.Id,Title=r.Title,RemindAt=r.RemindAt,DeadlineId=r.DeadlineId,ActionId=r.ActionId};_db.DocumentReminders.Add(x);await _db.SaveChangesAsync();return Ok(x);}
    [HttpPut("reminders/{reminderId:guid}/read")]
    public async Task<IActionResult> ReadReminder(Guid documentId,Guid reminderId){var d=await Owned(documentId);if(d==null)return NotFound();var r=await _db.DocumentReminders.FirstOrDefaultAsync(x=>x.Id==reminderId&&x.DocumentId==d.Id);if(r==null)return NotFound();r.IsRead=true;await _db.SaveChangesAsync();return Ok();}

    [HttpPost("share")]
    public async Task<IActionResult> Share(Guid documentId,ShareRequest r){var d=await Owned(documentId);if(d==null)return NotFound();var s=new DocumentShare{Id=Guid.NewGuid(),DocumentId=d.Id,Token=Convert.ToHexString(Guid.NewGuid().ToByteArray())+Convert.ToHexString(Guid.NewGuid().ToByteArray()),ExpiresAt=r.ExpiresAt};_db.DocumentShares.Add(s);await _db.SaveChangesAsync();return Ok(new{token=s.Token,expiresAt=s.ExpiresAt});}
    [HttpDelete("share/{token}")]
    public async Task<IActionResult> RevokeShare(Guid documentId,string token){var d=await Owned(documentId);if(d==null)return NotFound();var s=await _db.DocumentShares.FirstOrDefaultAsync(x=>x.DocumentId==d.Id&&x.Token==token);if(s==null)return NotFound();s.Revoked=true;await _db.SaveChangesAsync();return Ok();}

    [HttpPost("view")]
    public async Task<IActionResult> Viewed(Guid documentId){var d=await Owned(documentId);if(d==null)return NotFound();d.LastViewedAt=DateTime.UtcNow;await _db.SaveChangesAsync();return Ok();}
}
