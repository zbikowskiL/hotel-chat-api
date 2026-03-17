namespace HotelChatApi.Models;

public record ChatRequest
{
    public string Message { get; init; } = string.Empty;
    public string HotelId { get; init; } = string.Empty;
    public string? SessionId { get; init; }
    public string? Language { get; init; } = "pl";
}

public record ChatResponse
{
    public string Reply { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string HotelId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public record N8nRequest
{
    public string Message { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string HotelId { get; init; } = string.Empty;
    public string Language { get; init; } = "pl";
}

public record N8nResponse
{
    public bool Success { get; init; }
    public string Reply { get; init; } = string.Empty;
    public string? Error { get; init; }
}

public class N8nOptions
{
    public string WebhookUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
