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

        public TempestiveController(TempestiveService service)
        {
            _service = service;
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

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            // Procesa y genera el reporte basado en el template
            var reportStream = _service.ProcessExcel(stream, month, year);

            // Devuelve el archivo Excel como respuesta descargable
            var fileName = $"rapportino_{month}_{year}.xlsx";
            return File(reportStream, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        fileName);
        }
    }
}
