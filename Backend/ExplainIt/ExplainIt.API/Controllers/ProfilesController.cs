using ExplainIt.API.Data;
using ExplainIt.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ExplainIt.API.DTOs;

namespace ExplainIt.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfilesController : ControllerBase
    {
        private readonly ExplainItDbContext _context;

        public ProfilesController(ExplainItDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var profile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.Id == userId.Value);

            if (profile == null)
                return NotFound(new
                {
                    message = "Profile not found."
                });

            return Ok(profile);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProfile(
            CreateProfileRequest request)
        {
            var userId = GetUserId();

            if (userId == null)
                return Unauthorized();

            var existingProfile = await _context.Profiles
                .FirstOrDefaultAsync(p => p.Id == userId.Value);

            if (existingProfile != null)
            {
                return Conflict(new
                {
                    message = "Profile already exists."
                });
            }

            var profile = new Profile
            {
                Id = userId.Value,
                FullName = request.FullName,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow
            };

            _context.Profiles.Add(profile);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMyProfile),
                null,
                profile);
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