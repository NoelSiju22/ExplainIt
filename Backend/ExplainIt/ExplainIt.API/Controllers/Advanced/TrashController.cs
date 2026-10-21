using ExplainIt.API.Data;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using System.Security.Claims;
namespace ExplainIt.API.Controllers.Advanced;
[ApiController][Route("api/trash")][Authorize]
public class TrashController:ControllerBase{
 private readonly ExplainItDbContext _db; public TrashController(ExplainItDbContext db)=>_db=db; private Guid? U()=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:null;
 [HttpGet] public async Task<IActionResult> Get(){var u=U();if(u==null)return Unauthorized();return Ok(await _db.Documents.Where(d=>d.UserId==u&&d.DeletedAt!=null).OrderByDescending(d=>d.DeletedAt).Select(d=>new{d.Id,d.Title,d.DeletedAt}).ToListAsync());}
 [HttpPut("{id:guid}/restore")] public async Task<IActionResult> Restore(Guid id){var u=U();if(u==null)return Unauthorized();var d=await _db.Documents.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==u);if(d==null)return NotFound();d.DeletedAt=null;_db.DocumentEvents.Add(new Models.DocumentEvent{Id=Guid.NewGuid(),DocumentId=d.Id,Type="restore",Description="Document restored from Trash."});await _db.SaveChangesAsync();return Ok();}
 [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id){var u=U();if(u==null)return Unauthorized();var d=await _db.Documents.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==u&&x.DeletedAt!=null);if(d==null)return NotFound();if(!string.IsNullOrWhiteSpace(d.FilePath)){var path=Path.Combine(Directory.GetCurrentDirectory(),"Uploads",d.FilePath);if(System.IO.File.Exists(path))System.IO.File.Delete(path);}_db.Documents.Remove(d);await _db.SaveChangesAsync();return Ok();}
}
