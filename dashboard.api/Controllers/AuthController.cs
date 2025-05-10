using Dashboard.Api.Models;
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
        // Validar si el usuario existe
        User? user = _context.Users.SingleOrDefault(u => u.Email == login.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(login.Password, user.Password))
        {
            return Unauthorized("Credenciales incorrectas.");
        }

        // Crear el JWT
        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        byte[] key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Sid, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User")
            }),
            Expires = DateTime.UtcNow.AddHours(1), // Establecer tiempo de expiración
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        return Ok(new { Token = jwtToken });
    }
}
