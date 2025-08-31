
using System.Security.Claims;
using Dashboard.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Api.Controllers
{
    [ApiController]
    [Route("api/tempestive")]
    [Authorize]
    public class TempestiveController : ControllerBase
    {
        private readonly TempestiveService _service;
        private readonly AppDbContext _context;

        public TempestiveController(TempestiveService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        // POST: api/tempestive/alfasReports
        [HttpPost("alfasReports")]
        public async Task<IActionResult> UploadReport(
            [FromForm] int month,
            [FromForm] int year,
            [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Nessun file caricato.");

            // Read User_Id from JWT claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID non trovato nel token.");

            // Search for the user in the database using userId
            var user = await _context.Users.FindAsync(int.Parse(userId));

            if (user == null)
                return Unauthorized("Utente non trovato.");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0; // Reset stream position

            // Process the Excel file
            return _service.GenerateAlfaReport(stream, month, year, user);
        }
    }
}