using Microsoft.EntityFrameworkCore;
using Fruittrack.Models;

namespace Fruittrack
{
    public class FruitTrackDbContext : DbContext
    {
        public DbSet<Truck> Trucks { get; set; }
        public DbSet<Farm> Farms { get; set; }
        public DbSet<Factory> Factories { get; set; }
        public DbSet<SupplyEntry> SupplyEntries { get; set; }
        public DbSet<FinancialSettlement> FinancialSettlements { get; set; }

        public FruitTrackDbContext(DbContextOptions<FruitTrackDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Truck
            modelBuilder.Entity<Truck>(entity =>
            {
                entity.HasKey(e => e.TruckId);
                entity.Property(e => e.TruckNumber).IsRequired();
            });

            // Farm
            modelBuilder.Entity<Farm>(entity =>
            {
                entity.HasKey(e => e.FarmId);
                entity.Property(e => e.FarmName).IsRequired();
            });

            // Factory
            modelBuilder.Entity<Factory>(entity =>
            {
                entity.HasKey(e => e.FactoryId);
                entity.Property(e => e.FactoryName).IsRequired();
            });

            // SupplyEntry
            modelBuilder.Entity<SupplyEntry>(entity =>
            {
                entity.HasKey(e => e.SupplyEntryId);
                entity.Property(e => e.EntryDate).IsRequired();
                entity.Property(e => e.FarmWeight).IsRequired();
                entity.Property(e => e.FarmDiscountRate).IsRequired();
                entity.Property(e => e.FarmPricePerTon).IsRequired();
                entity.Property(e => e.FactoryWeight).IsRequired();
                entity.Property(e => e.FactoryDiscountRate).IsRequired();
                entity.Property(e => e.FactoryPricePerTon).IsRequired();
                entity.Property(e => e.FreightCost).IsRequired();
                entity.Property(e => e.TransferFrom).IsRequired();
                entity.Property(e => e.TransferTo).IsRequired();

                entity.HasOne(e => e.Truck)
                      .WithMany(t => t.SupplyEntries)
                      .HasForeignKey(e => e.TruckId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Farm)
                      .WithMany(f => f.SupplyEntries)
                      .HasForeignKey(e => e.FarmId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Factory)
                      .WithMany(f => f.SupplyEntries)
                      .HasForeignKey(e => e.FactoryId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.FinancialSettlement)
                      .WithOne(fs => fs.SupplyEntry)
                      .HasForeignKey<FinancialSettlement>(fs => fs.SupplyEntryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // FinancialSettlement
            modelBuilder.Entity<FinancialSettlement>(entity =>
            {
                entity.HasKey(e => e.SettlementId);
                entity.Property(e => e.ExpectedAmount).IsRequired();
                entity.Property(e => e.ReceivedAmount).IsRequired();

                entity.HasOne(e => e.SupplyEntry)
                      .WithOne(se => se.FinancialSettlement)
                      .HasForeignKey<FinancialSettlement>(e => e.SupplyEntryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
} 