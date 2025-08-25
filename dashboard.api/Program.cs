using Microsoft.EntityFrameworkCore;
using Dashboard.Api.Infrastructure;
using Microsoft.OpenApi.Models;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalhostPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Add ports as needed
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Enable cookies
    });
});

// JWT Configuration
builder.Services.AddJwtConfiguration(builder.Configuration);

// Tempestive Service
builder.Services.AddScoped<Dashboard.Api.Services.TempestiveService>();

// Swagger Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Dashboard API", Version = "v1" });

    // Esto permite manejar bien IFormFile
    c.OperationFilter<FileUploadOperationFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

app.UseCors("LocalhostPolicy");

// Initialize the database and apply migrations
DatabaseInitializer.Initialize(app);


// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

// Authentication and Authorization Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
