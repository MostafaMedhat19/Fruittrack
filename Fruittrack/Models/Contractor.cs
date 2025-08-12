using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fruittrack.Models
{
    public class Contractor
    {
        public int ContractorId { get; set; }
        public string ContractorName { get; set; } = string.Empty;
        public int? ContractorCache { get; set; } 
        public string RelatedFramName { get; set; } = string.Empty;
        public string RelatedFactoryName { get; set; } = string.Empty;
    }
}
