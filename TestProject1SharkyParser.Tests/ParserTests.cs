using SharkyParser.Core;
namespace TestProject1SharkyParser.Tests;

public class ParserTests
{
    [Fact]
    public void ParseLine_ShouldParseBasicAppLog()
    {
        var parser = new LogParser();

        var entry = parser.ParseLine("2025-01-01 12:45:33,421 INFO [App] Hello");

        Assert.NotNull(entry);
        Assert.Equal("INFO", entry!.Level);
        Assert.Equal("App", entry.Source);
        Assert.Equal("Hello", entry.Message);
    }
}
