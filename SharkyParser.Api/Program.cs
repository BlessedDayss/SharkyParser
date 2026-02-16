using SharkyParser.Api.Infrastructure;
using SharkyParser.Api.Interfaces;
using SharkyParser.Api.Services;
using SharkyParser.Core;
using SharkyParser.Core.Infrastructure;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IAppLogger, ApiLogger>();
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

app.UseCors();
app.MapControllers();

app.Run();
