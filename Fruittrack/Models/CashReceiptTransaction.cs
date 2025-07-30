using System;
using System.ComponentModel.DataAnnotations;

namespace Fruittrack.Models
{
    public class CashReceiptTransaction
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الشخص مطلوب")]
        public string SourceName { get; set; }

        [Required(ErrorMessage = "المبلغ المستلم مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن يكون المبلغ المستلم أكبر من صفر")]
        public decimal ReceivedAmount { get; set; }

        [Required(ErrorMessage = "التاريخ مطلوب")]
        public DateTime Date { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "المبلغ المسدد لا يمكن أن يكون سالباً")]
        public decimal PaidBackAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "المتبقي لا يمكن أن يكون سالباً")]
        public decimal RemainingAmount { get; set; }
    }
} 