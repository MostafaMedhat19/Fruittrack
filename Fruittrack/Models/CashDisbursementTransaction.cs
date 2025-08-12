using System;
using System.ComponentModel.DataAnnotations;

namespace Fruittrack.Models
{
    public class CashDisbursementTransaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الجهة مطلوب")]
        public string EntityName { get; set; }

        [Required(ErrorMessage = "المبلغ مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن يكون المبلغ أكبر من صفر")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "التاريخ مطلوب")]
        public DateTime TransactionDate { get; set; }

        // Optional notes
        public string? Notes { get; set; }
    }
} 
