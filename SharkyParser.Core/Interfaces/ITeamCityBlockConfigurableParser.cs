namespace SharkyParser.Core.Interfaces;

/// <summary>
/// Implemented by TeamCity parser to allow filtering by selected block names.
/// </summary>
public interface ITeamCityBlockConfigurableParser
{
    /// <summary>
    /// Configures block names to include while parsing.
    /// Empty or null means no block filtering.
    /// </summary>
    void ConfigureBlocks(IEnumerable<string>? blocks);
}
