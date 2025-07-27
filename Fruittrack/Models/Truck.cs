namespace Fruittrack.Models
{
    public class Truck
    {
        public int TruckId { get; set; }
        public string TruckNumber { get; set; }
        public ICollection<SupplyEntry> SupplyEntries { get; set; }
    }
} 