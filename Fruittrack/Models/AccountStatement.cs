using System;
using System.Collections.Generic;

namespace Fruittrack.Models
{
    public class AccountStatement
    {
        public string EntityName { get; set; }
        public List<TransactionDetail> Transactions { get; set; } = new List<TransactionDetail>();
        public decimal TotalCredit { get; set; } // إجمالي المبالغ اللي ليه
        public decimal TotalDebit { get; set; }  // إجمالي المبالغ اللي عليه
        public decimal FinalBalance { get; set; } // الصافي النهائي
    }

    public class TransactionDetail
    {
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } // "صرف" or "استلام"
        public string Notes { get; set; }
        public decimal RunningBalance { get; set; }
    }
} 