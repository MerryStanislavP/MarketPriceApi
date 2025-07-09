using MarketPriceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketPriceApi.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetPrice> AssetPrices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Asset
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Symbol)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(e => e.Name)
                      .HasMaxLength(100);

                entity.HasIndex(e => e.Symbol)
                      .IsUnique();

                entity.HasMany(e => e.Prices)
                      .WithOne(p => p.Asset)
                      .HasForeignKey(p => p.AssetId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // AssetPrice
            modelBuilder.Entity<AssetPrice>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Price)
                      .HasColumnType("decimal(18,6)")
                      .IsRequired();

                entity.Property(e => e.Timestamp)
                      .IsRequired();

                entity.HasIndex(e => new { e.AssetId, e.Timestamp });
            });
        }
    }
}
