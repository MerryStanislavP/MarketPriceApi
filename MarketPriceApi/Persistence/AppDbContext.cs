using MarketPriceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MarketPriceApi.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetPrice> AssetPrices { get; set; }
        public DbSet<SyncLog> SyncLogs { get; set; }

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

                entity.Property(e => e.Provider)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.Kind)
                      .IsRequired()
                      .HasMaxLength(20);

                entity.Property(e => e.Exchange)
                      .HasMaxLength(50);

                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.Symbol)
                      .IsUnique();

                entity.HasIndex(e => new { e.Provider, e.Kind });

                entity.HasMany(e => e.Prices)
                      .WithOne(p => p.Asset)
                      .HasForeignKey(p => p.AssetId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // AssetPrice
            modelBuilder.Entity<AssetPrice>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Open)
                      .HasColumnType("decimal(18,6)")
                      .IsRequired();

                entity.Property(e => e.High)
                      .HasColumnType("decimal(18,6)")
                      .IsRequired();

                entity.Property(e => e.Low)
                      .HasColumnType("decimal(18,6)")
                      .IsRequired();

                entity.Property(e => e.Close)
                      .HasColumnType("decimal(18,6)")
                      .IsRequired();

                entity.Property(e => e.Volume)
                      .HasColumnType("decimal(18,6)")
                      .IsRequired();

                entity.Property(e => e.Interval)
                      .IsRequired()
                      .HasMaxLength(10);

                entity.Property(e => e.Provider)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.Timestamp)
                      .IsRequired();

                entity.HasIndex(e => new { e.AssetId, e.Timestamp });
                entity.HasIndex(e => new { e.AssetId, e.Interval, e.Timestamp });
                entity.HasIndex(e => e.Timestamp);
            });

            // SyncLog
            modelBuilder.Entity<SyncLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Operation)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(e => e.StartedAt)
                      .IsRequired()
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.ErrorMessage)
                      .HasMaxLength(1000);

                entity.Property(e => e.Provider)
                      .HasMaxLength(50);

                entity.Property(e => e.Kind)
                      .HasMaxLength(20);

                entity.HasIndex(e => new { e.Operation, e.StartedAt });
                entity.HasIndex(e => e.IsSuccess);
                entity.HasIndex(e => e.AssetId);
                entity.HasIndex(e => e.Symbol);

                entity.HasOne(e => e.Asset)
                      .WithMany()
                      .HasForeignKey(e => e.AssetId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
