using Microsoft.EntityFrameworkCore;
using SharkyParser.Api.Data;
using SharkyParser.Api.Infrastructure;
using SharkyParser.Api.Interfaces;
using SharkyParser.Api.Services;
using SharkyParser.Core;
using SharkyParser.Core.Enums;
using SharkyParser.Core.Infrastructure;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;
using ILogger = SharkyParser.Core.Interfaces.ILogger;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Large File Support ─────────────────────────────────────────────────────
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // 100MB
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
});


builder.Services.AddSingleton<ApiLogger>();
builder.Services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ApiLogger>());
builder.Services.AddSingleton<ILogParserRegistry, LogParserRegistry>();
builder.Services.AddSingleton<ILogParserFactory, LogParserFactory>();
builder.Services.AddTransient<InstallationLogParser>();
builder.Services.AddTransient<UpdateLogParser>();
builder.Services.AddTransient<IISLogParser>();
builder.Services.AddSingleton<ILogAnalyzer, LogAnalyzer>();
builder.Services.AddScoped<ILogParsingService, LogParsingService>();
builder.Services.AddScoped<IChangelogService, ChangelogService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IGitHubAuthService, GitHubAuthService>();
builder.Services.AddSingleton<ICopilotAgentService, CopilotAgentService>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Database Auto-Migration ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // In a real production app, we'd use migrations. 
    // Here we ensure the DB is created to match the model.
    db.Database.EnsureCreated();
}

app.UseCors();
app.MapControllers();

await app.RunAsync();

