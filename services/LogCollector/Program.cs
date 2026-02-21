var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/logs", (SecurityLog log) =>{
    Console.WriteLine($"[log recieved] {log.source} | {log.Severity} | {log.RawMessage}");
    return Results.Accepted($"/log/{log.Id}", log);
});

//health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow}));
app.Run();