using ExplainIt.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ExplainIt.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly SupabaseAuthService _supabaseAuthService;

        public AuthController(
            SupabaseAuthService supabaseAuthService)
        {
            _supabaseAuthService = supabaseAuthService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    message = "Email and password are required."
                });
            }

            var result = await _supabaseAuthService.LoginAsync(
                request.Email,
                request.Password);

            if (result == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            return Ok(result);
        }


        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Name, email, and password are required." });
            if (request.Password.Length < 8)
                return BadRequest(new { message = "Password must be at least 8 characters long." });
            try
            {
                var result = await _supabaseAuthService.RegisterAsync(request.Name.Trim(), request.Email.Trim(), request.Password);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("test")]
        [Authorize]
        public IActionResult Test()
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            return Ok(new
            {
                authenticated = true,
                userId = userId
            });
        }
    }

    public class RegisterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}