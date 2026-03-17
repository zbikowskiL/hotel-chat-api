using System.Net.Http.Json;
using HotelChatApi.Models;
using Microsoft.Extensions.Options;

namespace HotelChatApi.Services;

public interface IN8nService
{
    Task<N8nResponse> SendMessageAsync(N8nRequest request);
}

public class N8nService : IN8nService
{
    private readonly HttpClient _http;
    private readonly N8nOptions _options;
    private readonly ILogger<N8nService> _logger;

    public N8nService(HttpClient http, IOptions<N8nOptions> options, ILogger<N8nService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<N8nResponse> SendMessageAsync(N8nRequest request)
    {
        try
        {
            _logger.LogInformation(
                "[N8n] Wysyłanie → Hotel: {HotelId}, Session: {SessionId}, Msg: {Msg}",
                request.HotelId, request.SessionId, request.Message);

            var response = await _http.PostAsJsonAsync(_options.WebhookUrl, request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[N8n] Błąd {StatusCode}: {Body}", response.StatusCode, body);
                return new N8nResponse { Success = false, Error = $"n8n error {response.StatusCode}: {body}" };
            }

            var result = await response.Content.ReadFromJsonAsync<N8nWebhookResult>();
            _logger.LogInformation("[N8n] Odpowiedź: {Reply}", result?.Reply);

            return new N8nResponse
            {
                Success = true,
                Reply = result?.Reply ?? "Przepraszam, nie mogę teraz odpowiedzieć."
            };
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("[N8n] Timeout po {Timeout}s", _options.TimeoutSeconds);
            return new N8nResponse { Success = false, Error = $"Timeout po {_options.TimeoutSeconds}s" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[N8n] Nieoczekiwany błąd");
            return new N8nResponse { Success = false, Error = ex.Message };
        }
    }

    private record N8nWebhookResult
    {
        public string Reply { get; init; } = string.Empty;
        public string? SessionId { get; init; }
    }
}
