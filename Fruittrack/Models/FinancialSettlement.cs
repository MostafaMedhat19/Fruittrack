namespace Fruittrack.Models
{
    public class FinancialSettlement
    {
        public int SettlementId { get; set; }

        public int SupplyEntryId { get; set; }
        public SupplyEntry SupplyEntry { get; set; }

        public decimal ExpectedAmount { get; set; }
        public decimal ReceivedAmount { get; set; }
    }
} 