using ExplainIt.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExplainIt.API.Controllers.Dashboard;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ExplainItDbContext _db;
    public DashboardController(ExplainItDbContext db)=>_db=db;
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var uid=UserId(); if(uid==null)return Unauthorized(); var now=DateTime.UtcNow;
        var docs=await _db.Documents.Where(d=>d.UserId==uid&&d.DeletedAt==null).Include(d=>d.Category).ToListAsync();
        var ids=docs.Select(d=>d.Id).ToList();
        var actions=await _db.DocumentActions.Where(x=>ids.Contains(x.DocumentId)).ToListAsync();
        var deadlines=await _db.DocumentDeadlines.Where(x=>ids.Contains(x.DocumentId)).ToListAsync();
        var requirements=await _db.DocumentRequirements.Where(x=>ids.Contains(x.DocumentId)).ToListAsync();
        var reminders=await _db.DocumentReminders.Where(x=>ids.Contains(x.DocumentId)&&!x.IsRead&&x.RemindAt<=now.AddDays(30)).OrderBy(x=>x.RemindAt).Take(5).ToListAsync();
        var categoryCounts=docs.GroupBy(d=>d.Category?.Name??"Other").OrderByDescending(g=>g.Count()).Select(g=>new{name=g.Key,count=g.Count()}).ToList();
        return Ok(new{
            totalDocuments=docs.Count,
            totalActions=actions.Count,
            pendingActions=actions.Count(a=>!a.Status.Equals("Completed",StringComparison.OrdinalIgnoreCase)),
            pendingRequirements=requirements.Count(r=>!r.Completed),
            overdueDeadlines=deadlines.Count(d=>!d.IsCompleted&&d.Deadline<now),
            importantDocuments=docs.Count(d=>d.IsImportant),
            upcomingDeadlines=deadlines.Where(d=>!d.IsCompleted&&d.Deadline>=now).OrderBy(d=>d.Deadline).Take(5).Select(d=>new{d.Id,d.DocumentId,d.Title,d.Deadline,d.IsCompleted}),
            recentDocuments=docs.OrderByDescending(d=>d.UploadedAt).Take(5).Select(d=>new{d.Id,d.Title,d.CategoryId,categoryName=d.Category?.Name,d.UploadedAt,d.IsImportant,d.Status}),
            recentlyViewed=docs.Where(d=>d.LastViewedAt!=null).OrderByDescending(d=>d.LastViewedAt).Take(5).Select(d=>new{d.Id,d.Title,d.LastViewedAt}),
            needsAttention=docs.Where(d=>d.Status!="Completed" || d.Deadlines.Any(x=>!x.IsCompleted&&x.Deadline<now)).OrderBy(d=>d.LastViewedAt).Take(5).Select(d=>new{d.Id,d.Title,d.Status}),
            reminders=reminders.Select(r=>new{r.Id,r.DocumentId,r.Title,r.RemindAt,r.IsRead}),
            categories=categoryCounts
        });
    }
    private Guid? UserId()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
}
