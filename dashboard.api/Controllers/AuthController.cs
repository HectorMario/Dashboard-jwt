using Dashboard.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;

    public AuthController(IConfiguration configuration, AppDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] Login login)
    {
        // Validate if the user exists
        var user = _context.Users.SingleOrDefault(u => u.Email == login.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        // Create the JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        // Store JWT in an HttpOnly cookie
        Response.Cookies.Append("jwt", jwtToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Set to true in production
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddHours(5)
        });

        return Ok(new { message = "Login successful." });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        string? userId = User.FindFirst(ClaimTypes.Sid)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "User ID not found in token." });

        User? user = _context.Users.Find(int.Parse(userId));
        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.IsAdmin
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt");
        return Ok(new { message = "Logged out successfully." });
    }
}
