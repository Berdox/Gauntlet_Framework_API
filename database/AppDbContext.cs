using Microsoft.EntityFrameworkCore;
using gauntlet_framework_api.models;

namespace gauntlet_framework_api.database {
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
        public DbSet<RunRecord> RunRecords => Set<RunRecord>();
        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RunRecord>()
                .HasIndex(r => new { r.PlayerUniqueID, r.MapName, r.RouteName, r.EventName });

            modelBuilder.Entity<ApiKey>()
                .HasIndex(k => k.KeyHash)
                .IsUnique();
        }
    }
}
