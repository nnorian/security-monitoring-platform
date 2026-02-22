using Microsoft.EntityFrameworkCore;

public class AlbertDbContext : DbContext{

    public AlbertDbContext(DbContextOptions<AlbertDbContext> options) : base(options) {}
    public DbSet<Alert> Alerts => Set<Alert>();
}