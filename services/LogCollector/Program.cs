using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RabbitMqPublisher>(sp =>
    new RabbitMqPublisher(
        builder.Configuration["RABBITMQ_HOST"] ?? "localhost",
        builder.Configuration["RABBITMQ_USER"],
        builder.Configuration["RABBITMQ_PASS"]));

var app = builder.Build();
app.UseHttpMetrics();

app.UseSwagger();
app.UseSwaggerUI();

var logsIngested = Metrics.CreateCounter(
    "logs_ingested_total",
    "Total logs received",
    new CounterConfiguration { LabelNames = ["source", "severity"] });

app.MapPost("/logs", async (SecurityLog log, RabbitMqPublisher publisher) =>
{
    await publisher.PublishAsync(log);
    logsIngested.WithLabels(log.Source, log.Severity).Inc();
    return Results.Accepted($"/logs/{log.Id}", log);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapMetrics();

app.Run();
