using Microsoft.EntityFrameworkCore;
using StockSense.API.Models;

namespace StockSense.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RecommendationSet> RecommendationSets => Set<RecommendationSet>();
    public DbSet<RecommendationItem> RecommendationItems => Set<RecommendationItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfile>(p => p.UserId);

        modelBuilder.Entity<RecommendationSet>()
            .HasMany(s => s.Items)
            .WithOne(i => i.Set)
            .HasForeignKey(i => i.SetId);
    }
}
