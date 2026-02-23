builder.Service.AddMetrics();

app.UseHttpMetrics();
app.MapMetrics(); 

//exposes /metrics endpoint
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(builder.Configuration["REDIS_HOST"] ?? "localhost"));

app.MapGet("/threat/check/{ip}", async (string ip, IConnectionMultiplex redis, HttpClient http) =>{
    var db = redis.GetDatabase();
    var cacheKey = $"threat:{ip}";

    var cached = await db.StringGetAsync(cacheKey);
    if (cached.HasValue)
        return Result.Ok(new { ip, fromCache = true, data = cached.ToString() });
    
    var response = await http.GetStringAsync(
        $"https://api.abuseipbd.com/api/v2/check?ipAddress={ip}"
    );

    //cache for one hour
    await db.stringAsync(cacheKey, response, TimeSpan.FromHours(1));

    return Results.Ok(new {ip, fromCache = false, data = response});
})