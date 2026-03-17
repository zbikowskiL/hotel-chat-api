namespace HotelChatApi.Services;

public interface ISessionService
{
    string CreateSession(string hotelId);
    bool IsValid(string sessionId);
}

public class SessionService : ISessionService
{
    private readonly HashSet<string> _sessions = [];
    private readonly ILogger<SessionService> _logger;

    public SessionService(ILogger<SessionService> logger)
    {
        _logger = logger;
    }

    public string CreateSession(string hotelId)
    {
        var sessionId = $"{hotelId}_{Guid.NewGuid():N}";
        _sessions.Add(sessionId);
        _logger.LogInformation("[Session] Nowa sesja: {SessionId}", sessionId);
        return sessionId;
    }

    public bool IsValid(string sessionId) =>
        !string.IsNullOrWhiteSpace(sessionId);
}
