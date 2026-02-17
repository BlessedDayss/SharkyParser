using Microsoft.EntityFrameworkCore;
using SharkyParser.Api.Data;
using SharkyParser.Api.Data.Repositories;
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

builder.Services.AddScoped<IFileRepository, FileRepository>();

// ── Large file support ─────────────────────────────────────────────────────
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 100 * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
    o.MultipartBodyLengthLimit = 100 * 1024 * 1024);

// ── Logger ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ApiLogger>();
builder.Services.AddSingleton<ILogger>(sp => sp.GetRequiredService<ApiLogger>());

// ── Parsers ────────────────────────────────────────────────────────────────
builder.Services.AddTransient<InstallationLogParser>();
builder.Services.AddSingleton<UpdateLogParser>();
builder.Services.AddSingleton<IISLogParser>();

// ILogParserRegistry registered as a factory so the registry is populated
// correctly for any ServiceProvider that resolves it (including scoped ones).
builder.Services.AddSingleton<ILogParserRegistry>(sp =>
{
    var registry = new LogParserRegistry();
    registry.Register(LogType.Installation, () => sp.GetRequiredService<InstallationLogParser>());
    registry.Register(LogType.Update,       () => sp.GetRequiredService<UpdateLogParser>());
    registry.Register(LogType.IIS,          () => sp.GetRequiredService<IISLogParser>());
    return registry;
});

builder.Services.AddSingleton<ILogParserFactory, LogParserFactory>();
builder.Services.AddSingleton<ILogAnalyzer, LogAnalyzer>();

// ── Application services ───────────────────────────────────────────────────
builder.Services.AddScoped<ILogParsingService, LogParsingService>();
builder.Services.AddScoped<IChangelogService, ChangelogService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IGitHubAuthService, GitHubAuthService>();
builder.Services.AddSingleton<ICopilotAgentService, CopilotAgentService>();

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// ── Auto-migration ─────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors();
app.MapControllers();

await app.RunAsync();
