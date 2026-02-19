using FluentAssertions;
using Moq;
using SharkyParser.Core.Interfaces;
using SharkyParser.Core.Parsers;
using Xunit;
using ILogger = SharkyParser.Core.Interfaces.ILogger;

namespace SharkyParser.Tests.Parsers;

public class TeamCityLogParserTests
{
    private readonly Mock<ILogger> _logger;
    private readonly TeamCityLogParser _parser;

    public TeamCityLogParserTests()
    {
        _logger = new Mock<ILogger>();
        _parser = new TeamCityLogParser(_logger.Object);
    }

    // ── Timestamped lines ─────────────────────────────────────────────────

    [Fact]
    public void ParseLine_WithTimeAndMessage_ReturnsEntry()
    {
        var entry = _parser.ParseLine("[10:30:45] Starting build...");

        entry.Should().NotBeNull();
        entry!.Message.Should().Be("Starting build...");
        entry.Level.Should().Be("INFO");
    }

    [Fact]
    public void ParseLine_WithErrorMarker_ReturnsErrorLevel()
    {
        var entry = _parser.ParseLine("[10:30:45]E: Compilation failed");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("ERROR");
        entry.Message.Should().Be("Compilation failed");
    }

    [Fact]
    public void ParseLine_WithWarningMarker_ReturnsWarnLevel()
    {
        var entry = _parser.ParseLine("[10:30:45]W: Deprecated API usage");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("WARN");
        entry.Message.Should().Be("Deprecated API usage");
    }

    [Fact]
    public void ParseLine_WithInfoMarker_ReturnsInfoLevel()
    {
        var entry = _parser.ParseLine("[10:30:45]i: Build configuration loaded");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("INFO");
        entry.Message.Should().Be("Build configuration loaded");
    }

    [Fact]
    public void ParseLine_WithStepInfo_ExtractsStepField()
    {
        var entry = _parser.ParseLine("[10:30:45] : [Step 1/3] Running dotnet build");

        entry.Should().NotBeNull();
        entry!.Fields.Should().ContainKey("Step");
        entry.Fields["Step"].Should().Be("Step 1/3");
        entry.Message.Should().Be("Running dotnet build");
    }

    [Fact]
    public void ParseLine_WithFullDatetime_ParsesCorrectly()
    {
        var entry = _parser.ParseLine("[2026-02-18 10:30:45] Build started");

        entry.Should().NotBeNull();
        entry!.Timestamp.Should().Be(new DateTime(2026, 2, 18, 10, 30, 45));
        entry.Message.Should().Be("Build started");
    }

    // ── Service messages ──────────────────────────────────────────────────

    [Fact]
    public void ParseLine_ServiceMessage_Message_ParsesTextAndStatus()
    {
        var entry = _parser.ParseLine("##teamcity[message text='Build completed' status='NORMAL']");

        entry.Should().NotBeNull();
        entry!.Message.Should().Be("Build completed");
        entry.Level.Should().Be("INFO");
        entry.Fields["MessageType"].Should().Be("message");
    }

    [Fact]
    public void ParseLine_ServiceMessage_ErrorStatus_ReturnsError()
    {
        var entry = _parser.ParseLine("##teamcity[message text='Fatal crash' status='ERROR']");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("ERROR");
        entry.Message.Should().Be("Fatal crash");
    }

    [Fact]
    public void ParseLine_ServiceMessage_BuildProblem_ReturnsError()
    {
        var entry = _parser.ParseLine("##teamcity[buildProblem description='Connection refused' identity='DB_001']");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("ERROR");
        entry.Message.Should().Contain("Connection refused");
        entry.Fields["ProblemId"].Should().Be("DB_001");
    }

    [Fact]
    public void ParseLine_ServiceMessage_TestFailed_ReturnsErrorWithTestName()
    {
        var entry = _parser.ParseLine("##teamcity[testFailed name='MyApp.Tests.LoginTest' message='Assertion failed']");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("ERROR");
        entry.Message.Should().Contain("MyApp.Tests.LoginTest");
        entry.Message.Should().Contain("Assertion failed");
        entry.Fields["TestName"].Should().Be("MyApp.Tests.LoginTest");
    }

    [Fact]
    public void ParseLine_ServiceMessage_TestStarted_ReturnsDebug()
    {
        var entry = _parser.ParseLine("##teamcity[testStarted name='MyApp.Tests.HomeTest']");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("DEBUG");
        entry.Fields["TestName"].Should().Be("MyApp.Tests.HomeTest");
    }

    [Fact]
    public void ParseLine_ServiceMessage_TestFinished_CapturesDuration()
    {
        var entry = _parser.ParseLine("##teamcity[testFinished name='MyApp.Tests.HomeTest' duration='1500']");

        entry.Should().NotBeNull();
        entry!.Fields["Duration"].Should().Be("1500");
    }

    [Fact]
    public void ParseLine_ServiceMessage_TestIgnored_ReturnsWarn()
    {
        var entry = _parser.ParseLine("##teamcity[testIgnored name='SkippedTest' message='Known issue']");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("WARN");
        entry.Message.Should().Contain("SkippedTest");
    }

    [Fact]
    public void ParseLine_ServiceMessage_Block_RendersName()
    {
        var entry = _parser.ParseLine("##teamcity[blockOpened name='Compilation']");

        entry.Should().NotBeNull();
        entry!.Message.Should().Contain("Compilation");
    }

    [Fact]
    public void ParseLine_ServiceMessage_BuildStatus_Failure()
    {
        var entry = _parser.ParseLine("##teamcity[buildStatus status='FAILURE' text='Tests failed: 3']");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("ERROR");
        entry.Message.Should().Contain("Tests failed: 3");
    }

    [Fact]
    public void ParseLine_ServiceMessage_WithEscapedPipes_Unescapes()
    {
        var entry = _parser.ParseLine("##teamcity[message text='Value with |'quotes|' inside' status='NORMAL']");

        entry.Should().NotBeNull();
        entry!.Message.Should().Contain("'");
    }

    [Fact]
    public void ParseLine_ServiceMessage_WithFlowId_CapturesIt()
    {
        var entry = _parser.ParseLine("##teamcity[testStarted name='Test1' flowId='flow42']");

        entry.Should().NotBeNull();
        entry!.Fields["FlowId"].Should().Be("flow42");
    }

    // ── Plain text / edge cases ───────────────────────────────────────────

    [Fact]
    public void ParseLine_PlainText_DetectsLevel()
    {
        var entry = _parser.ParseLine("ERROR: Out of memory");

        entry.Should().NotBeNull();
        entry!.Level.Should().Be("ERROR");
    }

    [Fact]
    public void ParseLine_EmptyLine_ReturnsNull()
    {
        var entry = _parser.ParseLine("   ");
        entry.Should().BeNull();
    }

    // ── ParseFile integration ─────────────────────────────────────────────

    [Fact]
    public void ParseFile_MixedContent_ParsesAll()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllLines(tempFile, new[]
        {
            "[10:00:01] Build started",
            "[10:00:02]E: Failed to restore packages",
            "##teamcity[testFailed name='SmokeTest' message='timeout']",
            "[10:00:05] : [Step 2/3] Running tests",
            "##teamcity[buildStatus status='FAILURE' text='Build failed']",
            "",
            "Some plain text"
        });

        try
        {
            var entries = _parser.ParseFile(tempFile).ToList();

            entries.Should().HaveCount(6);

            entries[0].Level.Should().Be("INFO");
            entries[1].Level.Should().Be("ERROR");
            entries[2].Level.Should().Be("ERROR");           // testFailed
            entries[3].Fields["Step"].Should().Be("Step 2/3");
            entries[4].Level.Should().Be("ERROR");           // FAILURE status
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetColumns_ReturnsExpectedColumns()
    {
        var columns = _parser.GetColumns();

        columns.Should().Contain(c => c.Name == "Step");
        columns.Should().Contain(c => c.Name == "MessageType");
        columns.Should().Contain(c => c.Name == "TestName");
    }
}
