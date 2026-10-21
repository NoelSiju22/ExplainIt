using ExplainIt.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExplainIt.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseController : ControllerBase
    {
        private readonly ExplainItDbContext _context;

        public DatabaseController(ExplainItDbContext context)
        {
            _context = context;
        }

        [HttpGet("test")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                await _context.Database.OpenConnectionAsync();

                await _context.Database.CloseConnectionAsync();

                return Ok(new
                {
                    success = true,
                    message = "ExplainIt database connection is working."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Database connection failed.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}