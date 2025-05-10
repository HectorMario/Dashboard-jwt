using Microsoft.AspNetCore.Mvc;
using Dashboard.Api.Models;
namespace Dashboard.Api.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

       
    }
}
