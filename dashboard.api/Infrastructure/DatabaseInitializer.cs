using Microsoft.EntityFrameworkCore;
using Dashboard.Api.Models;

namespace Dashboard.Api.Infrastructure
{
    public static class DatabaseInitializer
    {
        public static void Initialize(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Asegurar DB y aplicar migraciones
            context.Database.Migrate();

            // Seed de usuario por defecto
            SeedDefaultUser(context, config);
        }

        private static void SeedDefaultUser(AppDbContext context, IConfiguration config)
        {
            var userDefault = config.GetSection("UserDefault").Get<User>();

            if (userDefault == null ||
                string.IsNullOrWhiteSpace(userDefault.Email) ||
                string.IsNullOrWhiteSpace(userDefault.Password))
            {
                throw new Exception("UserDefault configuration is invalid.");
            }

            // Verificar si ya existe
            var adminUser = context.Users.FirstOrDefault(u => u.Email == userDefault.Email);

            if (adminUser == null)
            {
                var newAdminUser = new User
                {
                    FirstName = userDefault.FirstName,
                    LastName = userDefault.LastName,
                    Username = userDefault.Username,
                    Email = userDefault.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(userDefault.Password),
                    IsAdmin = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Users.Add(newAdminUser);
                context.SaveChanges();
            }
        }
    }
}
