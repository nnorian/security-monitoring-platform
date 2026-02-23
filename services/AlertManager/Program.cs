builder.Services.AddHostService<LogConsumerService>();

builder.Services.AddDbContext<AlbertDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")
        ?? $"Host={builder.Configuration["POSTGRES_HOST"]};Database=security_events;Username={builder.Configuration["POSTGRES_USER"]};Password={builder.Configuration["POSTGRES_PASSWORD"]}"));