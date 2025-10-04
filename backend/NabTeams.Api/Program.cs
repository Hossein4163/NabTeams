using Microsoft.EntityFrameworkCore;
using NabTeams.Api.Data;
using NabTeams.Api.Services;
using NabTeams.Api.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=app.db"));

builder.Services.AddScoped<IChatRepository, EfChatRepository>();
builder.Services.AddScoped<IModerationLogStore, EfModerationLogStore>();
builder.Services.AddScoped<IUserDisciplineStore, EfUserDisciplineStore>();
builder.Services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();
builder.Services.AddSingleton<IModerationService, GeminiModerationService>();
builder.Services.AddScoped<ISupportKnowledgeBase, EfSupportKnowledgeBase>();
builder.Services.AddScoped<ISupportResponder, SupportResponder>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.MapControllers();

app.Run();
