namespace Fruittrack.Models
{
    public class Factory
    {
        public int FactoryId { get; set; }
        public string FactoryName { get; set; }
        public ICollection<SupplyEntry> SupplyEntries { get; set; }
    }
} 