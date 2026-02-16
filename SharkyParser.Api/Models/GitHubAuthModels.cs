using System.Text.Json.Serialization;

namespace SharkyParser.Api.Models;

/// <summary>
/// Raw JSON response from GitHub's device code endpoint.
/// </summary>
internal class DeviceCodeRaw
{
    [JsonPropertyName("device_code")] public string? DeviceCode { get; set; }
    [JsonPropertyName("user_code")] public string? UserCode { get; set; }
    [JsonPropertyName("verification_uri")] public string? VerificationUri { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("interval")] public int Interval { get; set; }
}

/// <summary>
/// Raw JSON response from GitHub's OAuth token polling endpoint.
/// </summary>
internal class TokenPollRaw
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    [JsonPropertyName("scope")] public string? Scope { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}
