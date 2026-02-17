namespace SharkyParser.Api.DTOs;

public record LogColumnDto(
    string Name,
    string Header,
    string? Description = null,
    bool IsPredefined = false
);
