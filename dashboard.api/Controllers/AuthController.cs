using Dashboard.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Dashboard.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly ILogger<AuthController> _logger;

        private const string DefaultCookieName = "jwt";
        private const int TokenExpirationHours = 1;
        private const int CookieExpirationHours = 5;

        public AuthController(IConfiguration configuration, AppDbContext context, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                var user = await AuthenticateUser(loginRequest);
                var jwtToken = GenerateJwtToken(user);
                
                SetJwtCookie(jwtToken);
                
                _logger.LogInformation("User {Email} logged in successfully", user.Email);
                
                return Ok(new LoginResponse { Message = "Login successful." });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", loginRequest.Email);
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", loginRequest.Email);
                return StatusCode(500, new ErrorResponse { Message = "An error occurred during login." });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new ErrorResponse { Message = "Invalid token." });
                    
                var user = await _context.Users.FindAsync(int.Parse(userId));
                
                if (user == null)
                    return NotFound(new ErrorResponse { Message = "User not found." });

                return Ok(new UserProfileResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    IsAdmin = user.IsAdmin
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return Unauthorized(new ErrorResponse { Message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            try
            {
                var cookieName = _configuration["Jwt:NameToken"] ?? DefaultCookieName;
                Response.Cookies.Delete(cookieName);
                
                _logger.LogInformation("User logged out successfully");
                
                return Ok(new BaseResponse { Message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new ErrorResponse { Message = "An error occurred during logout." });
            }
        }

        private async Task<User> AuthenticateUser(LoginRequest loginRequest)
        {
            if (string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.Password))
                throw new UnauthorizedAccessException("Email and password are required.");

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.Password))
                throw new UnauthorizedAccessException("Invalid credentials.");

            return user;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = GetJwtSettings();
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(CreateClaims(user)),
                Expires = DateTime.UtcNow.AddHours(TokenExpirationHours),
                Issuer = jwtSettings.Issuer,
                Audience = jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private IEnumerable<Claim> CreateClaims(User user)
        {
            return new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.IsAdmin ? Roles.Admin : Roles.User),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
        }

        private void SetJwtCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !IsDevelopmentEnvironment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(CookieExpirationHours)
            };

            var cookieName = _configuration["Jwt:NameToken"] ?? DefaultCookieName;
            Response.Cookies.Append(cookieName, token, cookieOptions);
        }

        private JwtSettings GetJwtSettings()
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT Secret Key is not configured.");

            return new JwtSettings
            {
                SecretKey = secretKey,
                Issuer = _configuration["Jwt:Issuer"] ?? string.Empty,
                Audience = _configuration["Jwt:Audience"] ?? string.Empty
            };
        }

        private bool IsDevelopmentEnvironment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        }

        // Classi per le risposte API
        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class LoginResponse : BaseResponse
        {
            public string? Token { get; set; }
        }

        public class UserProfileResponse
        {
            public int Id { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public bool IsAdmin { get; set; }
        }

        public class BaseResponse
        {
            public string Message { get; set; } = string.Empty;
        }

        public class ErrorResponse : BaseResponse
        {
            public string? Details { get; set; }
        }

        private class JwtSettings
        {
            public string SecretKey { get; set; } = string.Empty;
            public string Issuer { get; set; } = string.Empty;
            public string Audience { get; set; } = string.Empty;
        }

        private static class Roles
        {
            public const string Admin = "Admin";
            public const string User = "User";
        }
    }
}