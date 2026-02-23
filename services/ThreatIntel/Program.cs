builder.Service.AddMetrics();

app.UseHttpMetrics();
app.MapMetrics();