using NabTeams.Api.Services;
using NabTeams.Api.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IChatRepository, InMemoryChatRepository>();
builder.Services.AddSingleton<IModerationLogStore, InMemoryModerationLogStore>();
builder.Services.AddSingleton<IUserDisciplineStore, InMemoryUserDisciplineStore>();
builder.Services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();
builder.Services.AddSingleton<IModerationService, GeminiModerationService>();
builder.Services.AddSingleton<ISupportKnowledgeBase, InMemorySupportKnowledgeBase>();
builder.Services.AddSingleton<ISupportResponder, SupportResponder>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.MapControllers();

app.Run();
