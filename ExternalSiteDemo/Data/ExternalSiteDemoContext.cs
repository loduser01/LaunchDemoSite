using ExternalSiteDemo.Data;
using Microsoft.EntityFrameworkCore;

namespace ExternalSiteDemo.Models
{
    // A very simple DB Context. Adds DBInitializer to ensure automatic migrations!
    public class ExternalSiteDemoContext : DbContext
    {
        public static class DbInitializer
        {
            public static void Initialize(ExternalSiteDemoContext context) { context.Database.Migrate(); }
        }
        
        public ExternalSiteDemoContext(DbContextOptions<ExternalSiteDemoContext> options) : base(options) { }

        public DbSet<LabAction> LabLaunch { get; set; }
    }



}
