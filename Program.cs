using HotelChatApi.Models;
using HotelChatApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────
builder.Services.AddHttpClient<IN8nService, N8nService>();
builder.Services.Configure<N8nOptions>(builder.Configuration.GetSection("N8n"));
builder.Services.AddSingleton<ISessionService, SessionService>();

// ── CORS ──────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("HotelWidgetPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>() ?? ["*"];

        if (allowedOrigins.Contains("*"))
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        else
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

// ── Swagger ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Hotel Chat API",
        Version = "v1",
        Description = "API pośredniczące między Angular widgetem a n8n"
    });
});

var app = builder.Build();

// ── Middleware ─────────────────────────────────────────────
app.UseCors("HotelWidgetPolicy");

// ── API Key Middleware ─────────────────────────────────────
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/chat"))
    {
        // Przepuść OPTIONS (preflight CORS)
        if (context.Request.Method == "OPTIONS")
        {
            await next();
            return;
        }

        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        var validKey = app.Configuration["ApiKey"];

        if (string.IsNullOrEmpty(apiKey) || apiKey != validKey)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
    }
    await next();
});

// Swagger w trybie Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Chat API v1");
        c.RoutePrefix = "swagger";
    });
}

// ── Endpoints ─────────────────────────────────────────────

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow
}))
.WithName("HealthCheck")
.WithOpenApi();

// Główny endpoint czatu
app.MapPost("/api/chat", async (
    ChatRequest request,
    IN8nService n8nService,
    ISessionService sessionService) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
        return Results.BadRequest(new { error = "Wiadomość nie może być pusta." });

    if (string.IsNullOrWhiteSpace(request.HotelId))
        return Results.BadRequest(new { error = "HotelId jest wymagane." });

    var sessionId = string.IsNullOrWhiteSpace(request.SessionId)
        ? sessionService.CreateSession(request.HotelId)
        : request.SessionId;

    var result = await n8nService.SendMessageAsync(new N8nRequest
    {
        Message = request.Message,
        SessionId = sessionId,
        HotelId = request.HotelId,
        Language = request.Language ?? "pl"
    });

    if (!result.Success)
        return Results.Problem(
            detail: result.Error,
            statusCode: 502,
            title: "Błąd komunikacji z n8n");

    return Results.Ok(new ChatResponse
    {
        Reply = result.Reply,
        SessionId = sessionId,
        HotelId = request.HotelId,
        Timestamp = DateTime.UtcNow
    });
})
.WithName("SendChatMessage")
.WithOpenApi();

// OPTIONS preflight dla CORS
app.MapMethods("/api/chat", ["OPTIONS"], () => Results.Ok());

app.Run();
