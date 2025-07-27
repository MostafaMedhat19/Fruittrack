using System;

namespace Fruittrack.Models
{
    public class SupplyEntry
    {
        public int SupplyEntryId { get; set; }
        public DateTime EntryDate { get; set; }

        public int TruckId { get; set; }
        public Truck Truck { get; set; }

        public int FarmId { get; set; }
        public Farm Farm { get; set; }

        public decimal FarmWeight { get; set; }
        public decimal FarmDiscountRate { get; set; }
        public decimal FarmPricePerTon { get; set; }

        public int FactoryId { get; set; }
        public Factory Factory { get; set; }

        public decimal FactoryWeight { get; set; }
        public decimal FactoryDiscountRate { get; set; }
        public decimal FactoryPricePerTon { get; set; }

        public decimal FreightCost { get; set; }
        public string TransferFrom { get; set; }
        public string TransferTo { get; set; }

        public FinancialSettlement FinancialSettlement { get; set; }
    }
} 