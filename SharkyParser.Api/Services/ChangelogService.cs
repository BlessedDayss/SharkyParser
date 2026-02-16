using SharkyParser.Api.Interfaces;

namespace SharkyParser.Api.Services;

/// <summary>
/// Reads the changelog file from disk.
/// Searches multiple locations: solution root, project dir, output dir.
/// </summary>
public sealed class ChangelogService : IChangelogService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ChangelogService> _logger;

    private const string FallbackContent = "# Changelog\n\nNo changelog available.";

    public ChangelogService(IWebHostEnvironment env, ILogger<ChangelogService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string> GetChangelogAsync()
    {
        var path = ResolveChangelogPath();

        if (path == null)
        {
            return FallbackContent;
        }

        try
        {
            return await File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read changelog from {Path}", path);
            throw;
        }
    }

    private string? ResolveChangelogPath()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "Changelog.md")),
            Path.Combine(_env.ContentRootPath, "Changelog.md"),
            Path.Combine(AppContext.BaseDirectory, "Changelog.md")
        };

        var path = candidates.FirstOrDefault(File.Exists);
        if (path != null) return path;

        _logger.LogWarning("Changelog.md not found. Checked: {Paths}", string.Join(", ", candidates));
        return null;
    }
}
