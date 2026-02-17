namespace SharkyParser.Api.DTOs;

public record FileRecordDto(
    Guid Id,
    string FileName,
    long FileSize,
    string LogType,
    DateTime UploadedAt
);
