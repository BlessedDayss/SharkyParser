using FluentAssertions;
using SharkyParser.Cli.UI;

namespace SharkyParser.Tests.UI;

public class CommandHistoryTests
{
    [Fact]
    public void Add_Whitespace_DoesNotAddEntries()
    {
        var history = new CommandHistory();

        history.Add("");
        history.Add("   ");

        history.GetPrevious().Should().BeNull();
        history.GetNext().Should().BeNull();
    }

    [Fact]
    public void Add_ConsecutiveDuplicate_IgnoresDuplicate()
    {
        var history = new CommandHistory();

        history.Add("first");
        history.Add("first");
        history.Add("second");

        history.GetPrevious().Should().Be("second");
        history.GetPrevious().Should().Be("first");
        history.GetPrevious().Should().Be("first");
        history.GetNext().Should().Be("second");
    }

    [Fact]
    public void GetNext_AfterLast_ReturnsEmptyString()
    {
        var history = new CommandHistory();

        history.Add("one");
        history.Add("two");

        history.GetPrevious().Should().Be("two");
        history.GetNext().Should().Be(string.Empty);
        history.GetPrevious().Should().Be("two");
    }

    [Fact]
    public void ResetNavigation_MovesToEnd()
    {
        var history = new CommandHistory();

        history.Add("one");
        history.Add("two");

        history.GetPrevious().Should().Be("two");
        history.GetPrevious().Should().Be("one");

        history.ResetNavigation();

        history.GetPrevious().Should().Be("two");
    }
}
