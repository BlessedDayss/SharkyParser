using SharkyParser.Core.Enums;

namespace SharkyParser.Core.Interfaces;

/// <summary>
/// Implemented by parsers that require per-call runtime configuration
/// (e.g. StackTraceMode for InstallationLogParser).
/// Factory and commands use this interface instead of casting to concrete types,
/// preserving OCP and LSP.
/// </summary>
public interface IConfigurableParser
{
    /// <summary>Applies runtime configuration before parsing begins.</summary>
    void Configure(StackTraceMode mode);

    /// <summary>Human-readable summary of current configuration for display in UI.</summary>
    string GetConfigurationSummary();
}
