using System;
using System.ComponentModel.DataAnnotations;

namespace Fruittrack.Models
{
    public class CashDisbursementTransaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الجهة مطلوب")]
        public string EntityName { get; set; }

        [Required(ErrorMessage = "المبلغ المصروف مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن يكون المبلغ المصروف أكبر من صفر")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "التاريخ مطلوب")]
        public DateTime TransactionDate { get; set; }

        public string Notes { get; set; }

        // Calculated properties for display
        [Required(ErrorMessage = "المبلغ المصروف مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن يكون المبلغ المصروف أكبر من صفر")]
        public decimal Credit { get; set; } // ليه كام (How much the party should receive)

        [Required(ErrorMessage = "المبلغ المدفوع مطلوب")]
        [Range(0.00, double.MaxValue, ErrorMessage = "المبلغ المدفوع لا يمكن أن يكون سالبًا")]
        public decimal Debit { get; set; }  // عليه كام (How much the party owes)
        public decimal Balance { get; set; } // الصافي (Net balance)
     // الصافي (Net balance)
    }
} 
