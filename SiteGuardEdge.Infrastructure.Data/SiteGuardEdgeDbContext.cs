using Microsoft.EntityFrameworkCore;
using SiteGuardEdge.Domain.Entities;

namespace SiteGuardEdge.Infrastructure.Data;

public class SiteGuardEdgeDbContext : DbContext
{
    public SiteGuardEdgeDbContext(DbContextOptions<SiteGuardEdgeDbContext> options) : base(options)
    {
    }

    public DbSet<DetectionEvent> DetectionEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DetectionEvent>().ToTable("detection_logs");
    }
}