using Microsoft.AspNetCore.Mvc;
using Dashboard.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace Dashboard.Api.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Authorize] 
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/user
        [HttpGet]
        public ActionResult<IEnumerable<User>> GetUsers()
        {
            return Ok(_context.Users.ToList());
        }

        // GET: api/user/{id}
        [HttpGet("{id}")]
        public ActionResult<User> GetUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // POST: api/user
        [HttpPost]
        public ActionResult<User> CreateUser(User user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Password))
            {
                return BadRequest("Invalid user data.");
            }

            // Hash the password before saving
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.Users.Add(user);
            _context.SaveChanges();

            // Return 201 Created with location of new resource
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // PUT: api/user/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, User updatedUser)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.Email = updatedUser.Email;

            _context.SaveChanges();

            return NoContent();
        }

        // DELETE: api/user/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.Find(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            return NoContent();
        }
    }
}
