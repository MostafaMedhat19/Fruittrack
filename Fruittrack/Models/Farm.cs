namespace Fruittrack.Models
{
    public class Farm
    {
        public int FarmId { get; set; }
        public string FarmName { get; set; }
        public ICollection<SupplyEntry> SupplyEntries { get; set; }
    }
} 