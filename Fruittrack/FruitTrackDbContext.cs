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
        public DbSet<CashReceiptTransaction> CashReceiptTransactions { get; set; }
        public DbSet<CashDisbursementTransaction> CashDisbursementTransactions { get; set; }
        public DbSet<Contractor> Contractors { get; set; }
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
            
                entity.HasOne(e => e.Truck)
                      .WithMany(t => t.SupplyEntries)
                      .HasForeignKey(e => e.TruckId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Farm)
                      .WithMany(f => f.SupplyEntries)
                      .HasForeignKey(e => e.FarmId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Factory)
                      .WithMany(f => f.SupplyEntries)
                      .HasForeignKey(e => e.FactoryId)
                      .OnDelete(DeleteBehavior.SetNull);

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

            // CashReceiptTransaction
            modelBuilder.Entity<CashReceiptTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SourceName).IsRequired();
                entity.Property(e => e.ReceivedAmount).IsRequired();
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.Notes).HasDefaultValue(string.Empty);
            });
            modelBuilder.Entity<Contractor>(entity =>
            {
                entity.HasKey(e => e.ContractorId);
                entity.Property(e => e.ContractorName).IsRequired();
                entity.Property(e => e.ContractorCache).IsRequired(false);
                entity.Property(e=>e.RelatedFactoryName).IsRequired(false);
                entity.Property(e => e.RelatedFramName).IsRequired(false);
            });

            // CashDisbursementTransaction
            modelBuilder.Entity<CashDisbursementTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EntityName).IsRequired();
                entity.Property(e => e.TransactionDate).IsRequired();
                entity.Property(e => e.Amount).IsRequired();
            });
        }
    }
} 