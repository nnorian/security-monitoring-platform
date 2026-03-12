using Prometheus;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMetrics();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["REDIS_HOST"] ?? "localhost"));

var app = builder.Build();

app.UseHttpMetrics();
app.UseSwagger();
app.UseSwaggerUI();

//exposes /metrics endpoint
app.MapGet("/threat/check/{ip}", async (string ip, IConnectionMultiplexer redis, IHttpClientFactory httpFactory) => {
    var db = redis.GetDatabase();
    var cacheKey = $"threat:{ip}";

    var cached = await db.StringGetAsync(cacheKey);
    if (cached.HasValue)
        return Results.Ok(new { ip, fromCache = true, data = cached.ToString() });

    var http = httpFactory.CreateClient();
    var response = await http.GetStringAsync(
        $"https://api.abuseipdb.com/api/v2/check?ipAddress={ip}"
    );

    //cache for one hour
    await db.StringSetAsync(cacheKey, response, TimeSpan.FromHours(1));

    return Results.Ok(new { ip, fromCache = false, data = response });
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapMetrics();

app.Run();
