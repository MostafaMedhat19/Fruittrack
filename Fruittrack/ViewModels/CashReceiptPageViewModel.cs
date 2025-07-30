using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Fruittrack.Models;
using Microsoft.EntityFrameworkCore;

namespace Fruittrack.ViewModels
{
    public class CashReceiptPageViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly FruitTrackDbContext _dbContext;

        public CashReceiptPageViewModel(FruitTrackDbContext dbContext)
        {
            _dbContext = dbContext;

            Transactions = new ObservableCollection<CashReceiptTransaction>(_dbContext.CashReceiptTransactions.ToList());

            AddTransactionCommand = new RelayCommand(AddTransaction, CanAddTransaction);
         
            // Dummy data for testing
            if (!Transactions.Any())
            {
                Transactions.Add(new CashReceiptTransaction
                {
                    SourceName = "أحمد علي",
                    ReceivedAmount = 1000,
                    Date = DateTime.Now.AddDays(-2),
                    PaidBackAmount = 200,
                    RemainingAmount = 800
                });
            }
        }

        // Form fields
        private string _sourceName;
        public string SourceName
        {
            get => _sourceName;
            set { _sourceName = value; OnPropertyChanged(nameof(SourceName)); }
        }

        private decimal _receivedAmount;
        public decimal ReceivedAmount
        {
            get => _receivedAmount;
            set { _receivedAmount = value; OnPropertyChanged(nameof(ReceivedAmount)); OnPropertyChanged(nameof(RemainingAmount)); }
        }

        private DateTime _date = DateTime.Now;
        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(nameof(Date)); }
        }

        private decimal _paidBackAmount;
        public decimal PaidBackAmount
        {
            get => _paidBackAmount;
            set { _paidBackAmount = value; OnPropertyChanged(nameof(PaidBackAmount)); OnPropertyChanged(nameof(RemainingAmount)); }
        }

        private decimal _remainingAmount;
        public decimal RemainingAmount
        {
            get => ReceivedAmount - PaidBackAmount;
            set { _remainingAmount = value; OnPropertyChanged(nameof(RemainingAmount)); }
        }

        // DataGrid collection
        public ObservableCollection<CashReceiptTransaction> Transactions { get; set; }

        // Commands
        public ICommand AddTransactionCommand { get; }
        public ICommand NavigateToFinancialSettlementCommand { get; }

        // Add Transaction
        private void AddTransaction(object obj)
        {
            var transaction = new CashReceiptTransaction
            {
                SourceName = this.SourceName,
                ReceivedAmount = this.ReceivedAmount,
                Date = this.Date,
                PaidBackAmount = this.PaidBackAmount,
                RemainingAmount = this.RemainingAmount
            };

            _dbContext.CashReceiptTransactions.Add(transaction);
            _dbContext.SaveChanges();

            Transactions.Add(transaction);

            // Clear form
            SourceName = string.Empty;
            ReceivedAmount = 0;
            PaidBackAmount = 0;
            Date = DateTime.Now;
            OnPropertyChanged(nameof(RemainingAmount));
        }

        private bool CanAddTransaction(object obj)
        {
            return !string.IsNullOrWhiteSpace(SourceName) &&
                   ReceivedAmount > 0 &&
                   Date != default;
        }

      

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // IDataErrorInfo for validation
        public string Error => null;
        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(SourceName):
                        if (string.IsNullOrWhiteSpace(SourceName)) return "اسم الشخص مطلوب";
                        break;
                    case nameof(ReceivedAmount):
                        if (ReceivedAmount <= 0) return "المبلغ المستلم يجب أن يكون أكبر من صفر";
                        break;
                    case nameof(Date):
                        if (Date == default) return "التاريخ مطلوب";
                        break;
                }
                return null;
            }
        }
    }
} 