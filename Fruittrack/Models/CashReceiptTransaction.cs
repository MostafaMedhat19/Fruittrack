using System;
using System.ComponentModel.DataAnnotations;

namespace Fruittrack.Models
{
    public class CashReceiptTransaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الجهة مطلوب")]
        public string SourceName { get; set; }

        [Required(ErrorMessage = "المبلغ المستلم مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن يكون المبلغ المستلم أكبر من صفر")]
        public decimal ReceivedAmount { get; set; }

        [Required(ErrorMessage = "التاريخ مطلوب")]
        public DateTime Date { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
} 