using System.Text.Json.Serialization;

namespace SharkyParser.Api.Models;

/// <summary>
/// Request payload for the GitHub Models chat completion API.
/// </summary>
public class ChatCompletionRequest
{
    [JsonPropertyName("model")] public string Model { get; set; } = "";
    [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; set; } = [];
    [JsonPropertyName("max_tokens")] public int MaxTokens { get; set; }
    [JsonPropertyName("temperature")] public double Temperature { get; set; }
}

/// <summary>
/// A single message in the chat completion conversation.
/// </summary>
public class ChatMessage
{
    [JsonPropertyName("role")] public string Role { get; set; }
    [JsonPropertyName("content")] public string Content { get; set; }

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
    }
}

/// <summary>
/// Response from the GitHub Models chat completion API.
/// </summary>
public class ChatCompletionResponse
{
    [JsonPropertyName("choices")] public List<ChatChoice>? Choices { get; set; }
}

/// <summary>
/// A single choice in the chat completion response.
/// </summary>
public class ChatChoice
{
    [JsonPropertyName("message")] public ChatMessage? Message { get; set; }
}

/// <summary>
/// Source-generated JSON serialization context for chat models.
/// </summary>
[JsonSerializable(typeof(ChatCompletionRequest))]
[JsonSerializable(typeof(ChatCompletionResponse))]
internal partial class ChatJsonContext : JsonSerializerContext { }
