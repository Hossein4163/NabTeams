using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NabTeams.Api.Configuration;
using NabTeams.Api.Data;
using NabTeams.Api.Services;
using NabTeams.Api.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<AuthenticationSettings>(builder.Configuration.GetSection("Authentication"));
var authenticationSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>() ?? new AuthenticationSettings { Disabled = true };

builder.Services.AddHttpClient("gemini", client =>
{
    var options = builder.Configuration.GetSection("Gemini").Get<GeminiOptions>() ?? new GeminiOptions();
    client.BaseAddress = new Uri(options.Endpoint.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

if (!authenticationSettings.Disabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authenticationSettings.Authority;
            options.Audience = string.IsNullOrWhiteSpace(authenticationSettings.Audience) ? null : authenticationSettings.Audience;
            options.RequireHttpsMetadata = authenticationSettings.RequireHttpsMetadata;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = authenticationSettings.NameClaimType ?? ClaimTypes.NameIdentifier,
                RoleClaimType = authenticationSettings.RoleClaimType ?? ClaimTypes.Role,
                ValidateAudience = !string.IsNullOrWhiteSpace(authenticationSettings.Audience)
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthorizationPolicies.Admin, policy => policy.RequireRole(authenticationSettings.AdminRole));
    });
}
else
{
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthorizationPolicies.Admin, policy => policy.RequireAssertion(_ => true));
    });
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=nabteams;Username=nabteams;Password=nabteams";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IChatRepository, EfChatRepository>();
builder.Services.AddScoped<IModerationLogStore, EfModerationLogStore>();
builder.Services.AddScoped<IUserDisciplineStore, EfUserDisciplineStore>();
builder.Services.AddScoped<IAppealStore, EfAppealStore>();
builder.Services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();
builder.Services.AddSingleton<IModerationService, GeminiModerationService>();
builder.Services.AddScoped<ISupportKnowledgeBase, EfSupportKnowledgeBase>();
builder.Services.AddScoped<ISupportResponder, SupportResponder>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");

if (!authenticationSettings.Disabled)
{
    app.UseAuthentication();
}
else
{
    app.Use(async (context, next) =>
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            var debugUser = context.Request.Headers["X-Debug-User"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(debugUser))
            {
                var identity = new ClaimsIdentity("Debug");
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, debugUser));
                identity.AddClaim(new Claim("sub", debugUser));
                identity.AddClaim(new Claim(ClaimTypes.Name, debugUser));

                var debugEmail = context.Request.Headers["X-Debug-Email"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(debugEmail))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Email, debugEmail));
                }

                var rolesHeader = context.Request.Headers["X-Debug-Roles"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(rolesHeader))
                {
                    var roles = rolesHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var role in roles)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                        identity.AddClaim(new Claim("role", role));
                        identity.AddClaim(new Claim("roles", role));
                    }
                }

                context.User = new ClaimsPrincipal(identity);
            }
        }

        await next();
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();
