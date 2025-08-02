using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        public string TransactionType { get; set; }
        public string Notes { get; set; }
        public decimal Credit { get; set; }
        public decimal Debit { get; set; }
        public decimal Balance { get; set; }

        // These are for display only
        public string FormattedCredit { get; set; }
        public string FormattedDebit { get; set; }
        public string FormattedBalance { get; set; }
    }
} 