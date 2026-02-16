namespace SharkyParser.Api.DTOs;

/// <summary>
/// Incoming chat request from the client.
/// </summary>
public record ChatRequest(string Message, string? LogContext = null);
