using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Dashboard.Api.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; 
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? throw new Exception("JWT Secret Key is not configured."))
        )
    };
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


WebApplication app = builder.Build();

User? userDefault = builder.Configuration.GetSection("UserDefault").Get<User>();


if (userDefault == null || string.IsNullOrWhiteSpace(userDefault.Email) || string.IsNullOrWhiteSpace(userDefault.Password))
{
    throw new Exception("UserDefault configuration is invalid.");
}


using (IServiceScope scope = app.Services.CreateScope())
{
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Ensure the database is created
    context.Database.EnsureCreated();

    // Check if the admin user already exists
    User? adminUser = context.Users.FirstOrDefault(u => u.Email == userDefault.Email);

    if (adminUser == null)
    {
        // Create admin user if it doesn't exist
        User newAdminUser = new User
        {
            FirstName = userDefault.FirstName,
            LastName = userDefault.LastName,
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();  
app.UseAuthorization(); 

app.MapControllers();

app.Run();


