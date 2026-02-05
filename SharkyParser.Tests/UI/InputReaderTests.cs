using System.Reflection;
using FluentAssertions;
using SharkyParser.Cli.UI;

namespace SharkyParser.Tests.UI;

[Collection("Console")]
public class InputReaderTests
{
    [Fact]
    public void HelperMethods_CanBeInvoked()
    {
        InvokeHelper("ClearCurrentLine", "> ", 3);
        InvokeHelper("RedrawLine", "> ", "abc", 2);
    }

    private static void InvokeHelper(string methodName, params object[] args)
    {
        var method = typeof(InputReader).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();

        try
        {
            method!.Invoke(null, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is IOException or ArgumentOutOfRangeException)
        {
            // Ignore console limitations in test environments.
        }
    }
}
