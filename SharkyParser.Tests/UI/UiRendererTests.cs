using FluentAssertions;
using SharkyParser.Cli.UI;

namespace SharkyParser.Tests.UI;

[Collection("Console")]
public class UiRendererTests
{
    [Fact]
    public void Renderers_Show_DoNotThrow()
    {
        var action = () =>
        {
            BannerRenderer.Show();
            TipsRenderer.Show();
        };

        action.Should().NotThrow();
    }

    [Fact]
    public void SpinnerLoader_ShowStartup_DoesNotThrow()
    {
        var action = () => SpinnerLoader.ShowStartup();

        action.Should().NotThrow();
    }
}
