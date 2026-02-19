using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SharkyParser.Api.Controllers;
using SharkyParser.Api.DTOs;
using SharkyParser.Api.Interfaces;
using SharkyParser.Core.Enums;

namespace SharkyParser.Tests.Api;

public class LogsControllerTests
{
    [Fact]
    public async Task Parse_ForwardsBlocksToService()
    {
        var blocks = new List<string> { "Test-BeforeUpdate", "Update-App" };
        var parseResult = new ParseResultDto(
            Guid.NewGuid(),
            Array.Empty<LogEntryDto>(),
            Array.Empty<LogColumnDto>(),
            new LogStatisticsDto(0, 0, 0, 0, 0, true));

        var parsingService = new Mock<ILogParsingService>();
        parsingService
            .Setup(s => s.ParseFileAsync(
                It.IsAny<Stream>(),
                "sample.log",
                LogType.TeamCity,
                It.Is<IReadOnlyList<string>>(b => b.SequenceEqual(blocks)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parseResult);

        var controller = new LogsController(
            parsingService.Object,
            null!,
            Mock.Of<ILogger<LogsController>>());

        var content = System.Text.Encoding.UTF8.GetBytes("line");
        await using var stream = new MemoryStream(content);
        IFormFile file = new FormFile(stream, 0, content.Length, "file", "sample.log");

        var result = await controller.Parse(file, "TeamCity", blocks, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        parsingService.VerifyAll();
    }

    [Fact]
    public async Task GetEntries_ForwardsBlocksToService()
    {
        var id = Guid.NewGuid();
        var blocks = new List<string> { "Update-App" };
        var parseResult = new ParseResultDto(
            id,
            Array.Empty<LogEntryDto>(),
            Array.Empty<LogColumnDto>(),
            new LogStatisticsDto(0, 0, 0, 0, 0, true));

        var parsingService = new Mock<ILogParsingService>();
        parsingService
            .Setup(s => s.GetEntriesAsync(
                id,
                It.Is<IReadOnlyList<string>>(b => b.SequenceEqual(blocks)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parseResult);

        var controller = new LogsController(
            parsingService.Object,
            null!,
            Mock.Of<ILogger<LogsController>>());

        var result = await controller.GetEntries(id, blocks, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        parsingService.VerifyAll();
    }

    [Fact]
    public void Parse_BlocksParameter_UsesFromForm()
    {
        var method = typeof(LogsController).GetMethod(nameof(LogsController.Parse));
        method.Should().NotBeNull();

        var blocksParam = method!.GetParameters().Single(p => p.Name == "blocks");
        blocksParam.GetCustomAttribute<FromFormAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void GetEntries_BlocksParameter_UsesFromQueryNameBlocks()
    {
        var method = typeof(LogsController).GetMethod(nameof(LogsController.GetEntries));
        method.Should().NotBeNull();

        var blocksParam = method!.GetParameters().Single(p => p.Name == "blocks");
        var attr = blocksParam.GetCustomAttribute<FromQueryAttribute>();

        attr.Should().NotBeNull();
        attr!.Name.Should().Be("blocks");
    }
}
