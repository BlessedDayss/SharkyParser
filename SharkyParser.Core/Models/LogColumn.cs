namespace SharkyParser.Core.Models;

public record LogColumn(
    string Name,
    string Header,
    string? Description = null,
    bool IsPredefined = false
);
