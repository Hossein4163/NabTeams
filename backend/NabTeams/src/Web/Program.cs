using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter.Prometheus.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Polly;
using Polly.Extensions.Http;
using NabTeams.Application.Abstractions;
using NabTeams.Application.Common;
using NabTeams.Application.Registrations;
using NabTeams.Infrastructure.HealthChecks;
using NabTeams.Infrastructure.Monitoring;
using NabTeams.Infrastructure.Persistence;
using NabTeams.Infrastructure.Queues;
using NabTeams.Infrastructure.Services;
using NabTeams.Web.Background;
using NabTeams.Web.Configuration;
using NabTeams.Web.Hubs;
using NabTeams.Web.Middleware;
using Swashbuckle.AspNetCore.Annotations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NabTeams API",
        Version = "v1",
        Description = "پوشش‌دهندهٔ سرویس‌های چت، پشتیبانی و ثبت‌نام شرکت‌کنندگان، داوران و سرمایه‌گذاران."
    });
    options.EnableAnnotations();
});
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestPath | HttpLoggingFields.RequestMethod | HttpLoggingFields.ResponseStatusCode;
    logging.RequestBodyLogLimit = 0;
    logging.ResponseBodyLogLimit = 0;
});

builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<AuthenticationSettings>(builder.Configuration.GetSection("Authentication"));
builder.Services.Configure<PaymentGatewayOptions>(builder.Configuration.GetSection("Payments"));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notification"));
var authenticationSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>() ?? new AuthenticationSettings { Disabled = true };

builder.Services.AddHttpClient<GeminiBusinessPlanAnalyzer>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? "https://generativelanguage.googleapis.com"
            : options.BaseUrl.TrimEnd('/');
        client.BaseAddress = new Uri(baseUrl + "/");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.Timeout = TimeSpan.FromSeconds(20);
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient<IdPayPaymentGateway>((sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<PaymentGatewayOptions>>().Value;
        var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
            ? "https://api.idpay.ir"
            : options.BaseUrl.TrimEnd('/');
        client.BaseAddress = new Uri(baseUrl + "/");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.Timeout = TimeSpan.FromSeconds(20);
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient("notifications.sms", (sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<NotificationOptions>>().Value;
        if (!string.IsNullOrWhiteSpace(options.Sms.BaseUrl))
        {
            client.BaseAddress = new Uri(options.Sms.BaseUrl.TrimEnd('/') + "/");
        }
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

builder.Services.AddHttpClient("gemini", (sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<GeminiOptions>>().Value;
        var endpoint = string.IsNullOrWhiteSpace(options.Endpoint)
            ? "https://generativelanguage.googleapis.com/v1beta"
            : options.Endpoint.TrimEnd('/');
        client.BaseAddress = new Uri(endpoint + "/");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

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
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IChatRepository, EfChatRepository>();
builder.Services.AddScoped<IModerationLogStore, EfModerationLogStore>();
builder.Services.AddScoped<IUserDisciplineStore, EfUserDisciplineStore>();
builder.Services.AddScoped<IAppealStore, EfAppealStore>();
builder.Services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();
builder.Services.AddSingleton<IModerationService, GeminiModerationService>();
builder.Services.AddSingleton<IChatModerationQueue, ChatModerationQueue>();
builder.Services.AddScoped<ISupportKnowledgeBase, EfSupportKnowledgeBase>();
builder.Services.AddScoped<IRegistrationRepository, EfRegistrationRepository>();
builder.Services.AddSingleton<IRegistrationDocumentStorage, LocalRegistrationDocumentStorage>();
builder.Services.AddScoped<INotificationService, ExternalNotificationService>();
builder.Services.AddScoped<IPaymentGateway>(sp => sp.GetRequiredService<IdPayPaymentGateway>());
builder.Services.AddScoped<IBusinessPlanAnalyzer>(sp => sp.GetRequiredService<GeminiBusinessPlanAnalyzer>());
builder.Services.AddScoped<IRegistrationWorkflowService, RegistrationWorkflowService>();
builder.Services.AddScoped<ISupportResponder, SupportResponder>();
builder.Services.AddHostedService<ChatModerationWorker>();
builder.Services.AddSingleton<IMetricsRecorder, MetricsRecorder>();
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<GeminiHealthCheck>("gemini");

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("NabTeams.Api"))
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
        metrics.AddMeter(MetricsRecorder.MeterName);
        metrics.AddPrometheusExporter();
    });

var app = builder.Build();

var fileStorageOptions = app.Services.GetRequiredService<IOptions<FileStorageOptions>>().Value;
var uploadsRoot = ResolveStorageRoot(app.Environment.ContentRootPath, fileStorageOptions.RootPath);
Directory.CreateDirectory(uploadsRoot);
var requestPath = ResolveRequestPath(fileStorageOptions.PublicBaseUrl);
var contentTypeProvider = new FileExtensionContentTypeProvider();
var registrationDocumentStaticFiles = new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = requestPath,
    ContentTypeProvider = contentTypeProvider
};

await DatabaseInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseHttpLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseResponseCompression();
app.UseSecurityHeaders();
app.UseStaticFiles(registrationDocumentStaticFiles);

app.UseStaticFiles();

app.UseStaticFiles();

app.UseStaticFiles();

app.UseRouting();
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
            var debugUser = context.Request.Headers["X-Debug-User"].FirstOrDefault()
                ?? context.Request.Query["debug_user"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(debugUser))
            {
                var identity = new ClaimsIdentity("Debug");
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, debugUser));
                identity.AddClaim(new Claim("sub", debugUser));
                identity.AddClaim(new Claim(ClaimTypes.Name, debugUser));

                var debugEmail = context.Request.Headers["X-Debug-Email"].FirstOrDefault()
                    ?? context.Request.Query["debug_email"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(debugEmail))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Email, debugEmail));
                }

                var rolesHeader = context.Request.Headers["X-Debug-Roles"].FirstOrDefault()
                    ?? context.Request.Query["debug_roles"].FirstOrDefault();
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
app.MapHub<ChatHub>("/hubs/chat");
app.MapPrometheusScrapingEndpoint();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            results = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                error = entry.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
});

app.Run();

static string ResolveStorageRoot(string contentRoot, string? configuredRoot)
{
    if (string.IsNullOrWhiteSpace(configuredRoot))
    {
        return Path.Combine(contentRoot, "storage", "uploads");
    }

    var path = configuredRoot;
    if (!Path.IsPathRooted(path))
    {
        path = Path.Combine(contentRoot, path);
    }

    return Path.GetFullPath(path);
}

static PathString ResolveRequestPath(string? publicBaseUrl)
{
    if (string.IsNullOrWhiteSpace(publicBaseUrl))
    {
        return new PathString("/uploads");
    }

    if (Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var absolute))
    {
        return new PathString(absolute.AbsolutePath);
    }

    return new PathString(publicBaseUrl.StartsWith('/') ? publicBaseUrl : $"/{publicBaseUrl}");
}

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt));

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
