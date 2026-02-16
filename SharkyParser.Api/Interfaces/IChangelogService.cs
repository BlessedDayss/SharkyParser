namespace SharkyParser.Api.Interfaces;

/// <summary>
/// Abstraction for changelog file reading.
/// </summary>
public interface IChangelogService
{
    /// <summary>
    /// Returns the changelog content as a markdown string.
    /// </summary>
    Task<string> GetChangelogAsync();
}
