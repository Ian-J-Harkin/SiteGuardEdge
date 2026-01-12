using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SiteGuardEdge.Infrastructure.Data
{
    public class SiteGuardEdgeDbContextFactory : IDesignTimeDbContextFactory<SiteGuardEdgeDbContext>
    {
        public SiteGuardEdgeDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<SiteGuardEdgeDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                // Fallback to a default connection string if not found in appsettings.json
                // This is often used for design-time migrations when appsettings.json might not be directly accessible or configured.
                // For a real application, ensure your appsettings.json is correctly configured.
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SiteGuardEdgeDb;Trusted_Connection=True;MultipleActiveResultSets=true");
            }
            else
            {
                optionsBuilder.UseSqlServer(connectionString);
            }

            return new SiteGuardEdgeDbContext(optionsBuilder.Options);
        }
    }
}