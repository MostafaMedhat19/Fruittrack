using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Fruittrack.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Fruittrack.ViewModels
{
    public class AccountStatementViewModel : INotifyPropertyChanged
    {
        private readonly AccountStatement _accountStatement;
        private readonly FruitTrackDbContext _dbContext;
        private decimal _paidBackAmount;
        private decimal _receivedAmount;
        private decimal _remainingAmount;
        private decimal _totalRemainingAmount;

        public AccountStatementViewModel(AccountStatement accountStatement, FruitTrackDbContext dbContext)
        {
            _accountStatement = accountStatement;
            _dbContext = dbContext;
            GoBackCommand = new RelayCommand(GoBack);

            // Initialize transactions collection with formatted properties
            Transactions = new ObservableCollection<TransactionDetail>(_accountStatement.Transactions.Select(t => new TransactionDetail
            {
                TransactionDate = t.TransactionDate,
                TransactionType = t.TransactionType,
                Notes = t.Notes,
                Credit = t.Credit,
                Debit = t.Debit,
                Balance = t.Balance,
                FormattedPaidBackAmount = decimal.Parse(t.FormattedPaidBackAmount).ToString("C", new CultureInfo("ar-EG")),
                FormattedReceivedAmount = decimal.Parse(t.FormattedReceivedAmount).ToString("C", new CultureInfo("ar-EG")),
                FormattedRemainingAmount = decimal.Parse(t.FormattedRemainingAmount).ToString("C", new CultureInfo("ar-EG")),
                FormattedCredit = t.Credit.ToString("C", new CultureInfo("ar-EG")),
                FormattedDebit = t.Debit.ToString("C", new CultureInfo("ar-EG")),
                FormattedBalance = t.Balance.ToString("C", new CultureInfo("ar-EG"))
            }));

            var cashReceipt = _dbContext.CashReceiptTransactions
                .FirstOrDefault(x => x.SourceName == _accountStatement.EntityName);

            if (cashReceipt != null)
            {
                _paidBackAmount = cashReceipt.PaidBackAmount;
                _receivedAmount = cashReceipt.ReceivedAmount;
                _remainingAmount = Math.Abs(_paidBackAmount - _receivedAmount);
                _totalRemainingAmount = _dbContext.CashReceiptTransactions
                    .Where(x => x.SourceName == _accountStatement.EntityName)
                    .Sum(x => x.RemainingAmount);
            }
        }

        public string EntityName => _accountStatement.EntityName;
        public decimal TotalCredit => _accountStatement.TotalCredit;
        public decimal TotalDebit => _accountStatement.TotalDebit;
        public decimal FinalBalance => Math.Abs(TotalCredit - TotalDebit);
        public decimal PaidBackAmount => _paidBackAmount;
        public decimal ReceivedAmount => _receivedAmount;
        public decimal RemainingAmount => _remainingAmount;
        public decimal TotalRemainingAmount => _totalRemainingAmount;

        // Formatted properties for display
        public string FormattedTotalCredit => TotalCredit.ToString("C", new CultureInfo("ar-EG"));
        public string FormattedTotalDebit => TotalDebit.ToString("C", new CultureInfo("ar-EG"));
        public string FormattedFinalBalance => FinalBalance.ToString("C", new CultureInfo("ar-EG"));
        public string FormattedPaidBackAmount => PaidBackAmount.ToString("C", new CultureInfo("ar-EG"));
        public string FormattedReceivedAmount => ReceivedAmount.ToString("C", new CultureInfo("ar-EG"));
        public string FormattedRemainingAmount => RemainingAmount.ToString("C", new CultureInfo("ar-EG"));
        public string FormattedTotalRemainingAmount => TotalRemainingAmount.ToString("C", new CultureInfo("ar-EG"));
        public Brush FinalBalanceColor => FinalBalance > 0 ? Brushes.Green : Brushes.Black;

        private ObservableCollection<TransactionDetail> _transactions;
        public ObservableCollection<TransactionDetail> Transactions
        {
            get => _transactions;
            set
            {
                _transactions = value;
                OnPropertyChanged(nameof(Transactions));
            }
        }

        public ICommand GoBackCommand { get; }

        private void GoBack(object obj)
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is NavigationWindow navigationWindow)
            {
                navigationWindow.GoBack();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}