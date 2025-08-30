using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Dashboard.Api.Infrastructure
{
    public static class JwtConfigurator
    {
        private const string JwtSection = "Jwt";
        private const string DefaultCookieName = "jwt";
        
        public static IServiceCollection AddJwtConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            var jwtSettings = GetJwtSettings(configuration);
            ValidateJwtSettings(jwtSettings);

            services.AddAuthentication(ConfigureAuthenticationOptions)
                    .AddJwtBearer(options => ConfigureJwtBearerOptions(options, jwtSettings));

            return services;
        }

        private static JwtSettings GetJwtSettings(IConfiguration configuration)
        {
            var jwtSection = configuration.GetSection(JwtSection);
            
            return new JwtSettings
            {
                Issuer = jwtSection["Issuer"] ?? string.Empty,
                Audience = jwtSection["Audience"] ?? string.Empty,
                SecretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key is not configured."),
                CookieName = jwtSection["NameToken"] ?? DefaultCookieName
            };
        }

        private static void ValidateJwtSettings(JwtSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.SecretKey))
                throw new InvalidOperationException("JWT Secret Key is required.");

            if (string.IsNullOrWhiteSpace(settings.Issuer))
                throw new InvalidOperationException("JWT Issuer is required.");

            if (string.IsNullOrWhiteSpace(settings.Audience))
                throw new InvalidOperationException("JWT Audience is required.");
        }

        private static void ConfigureAuthenticationOptions(AuthenticationOptions options)
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }

        private static void ConfigureJwtBearerOptions(JwtBearerOptions options, JwtSettings settings)
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = CreateTokenValidationParameters(settings);
            options.Events = CreateJwtBearerEvents(settings.CookieName);
        }

        private static TokenValidationParameters CreateTokenValidationParameters(JwtSettings settings)
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,
                ValidateAudience = true,
                ValidAudience = settings.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
                ClockSkew = TimeSpan.Zero // Opzionale: rimuove lo skew di clock per validazione più stretta
            };
        }

        private static JwtBearerEvents CreateJwtBearerEvents(string cookieName)
        {
            return new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = GetTokenFromCookie(context, cookieName);
                    return Task.CompletedTask;
                }
            };
        }

        private static string? GetTokenFromCookie(MessageReceivedContext context, string cookieName)
        {
            return context.HttpContext.Request.Cookies[cookieName];
        }

        // Classe interna per rappresentare le impostazioni JWT
        private class JwtSettings
        {
            public string Issuer { get; set; } = string.Empty;
            public string Audience { get; set; } = string.Empty;
            public string SecretKey { get; set; } = string.Empty;
            public string CookieName { get; set; } = DefaultCookieName;
        }
    }
}