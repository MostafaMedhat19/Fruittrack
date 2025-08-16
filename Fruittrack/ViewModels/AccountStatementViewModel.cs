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
                ReceivedAmountValue = TryParseDecimal(t.FormattedReceivedAmount),
                DisbursedAmountValue = TryParseDecimal(t.FormattedDebit),
                FormattedPaidBackAmount = SafeFormat(t.FormattedPaidBackAmount),
                FormattedReceivedAmount = SafeFormat(t.FormattedReceivedAmount),
                FormattedRemainingAmount = SafeFormat(t.FormattedRemainingAmount),
                FormattedCredit = t.Credit.ToString("C0", new CultureInfo("ar-EG")),
                FormattedDebit = t.Debit.ToString("C0", new CultureInfo("ar-EG")),
                FormattedBalance = t.Balance.ToString("C0", new CultureInfo("ar-EG"))
            }));

            LoadAllEntityNames();
            ApplyEntityFilter(_accountStatement.EntityName);
        }

        // Filter support
        public ObservableCollection<string> AllEntityNames { get; private set; } = new();
        private string _selectedEntityFilter = string.Empty;
        public string SelectedEntityFilter
        {
            get => _selectedEntityFilter;
            set
            {
                _selectedEntityFilter = value;
                OnPropertyChanged(nameof(SelectedEntityFilter));
                ApplyEntityFilter(_selectedEntityFilter);
            }
        }

        private void LoadAllEntityNames()
        {
            var names = _dbContext.CashReceiptTransactions.Select(r => r.SourceName)
                .Union(_dbContext.CashDisbursementTransactions.Select(d => d.EntityName))
                .Distinct()
                .OrderBy(n => n)
                .ToList();
            AllEntityNames = new ObservableCollection<string>(names);
            OnPropertyChanged(nameof(AllEntityNames));
        }

        private void ApplyEntityFilter(string? entity)
        {
            var transactions = new List<TransactionDetail>();
            var receiptsQuery = _dbContext.CashReceiptTransactions.AsQueryable();
            var disbursementsQuery = _dbContext.CashDisbursementTransactions.AsQueryable();

            if (!string.IsNullOrWhiteSpace(entity))
            {
                receiptsQuery = receiptsQuery.Where(x => x.SourceName == entity);
                disbursementsQuery = disbursementsQuery.Where(x => x.EntityName == entity);
            }

            var receipts = receiptsQuery.ToList();
            var disbursements = disbursementsQuery.ToList();

            transactions.AddRange(receipts.Select(r => new TransactionDetail
            {
                TransactionDate = r.Date,
                PartyName = r.SourceName,
                TransactionType = "استلام",
                Notes = r.Notes,
                ReceivedAmountValue = r.ReceivedAmount,
                FormattedReceivedAmount = r.ReceivedAmount.ToString("N0", new CultureInfo("ar-EG")),
                FormattedDisbursedAmount = 0m.ToString("N0", new CultureInfo("ar-EG")),
                FormattedCredit = 0m.ToString("N0", new CultureInfo("ar-EG")),
                FormattedDebit = 0m.ToString("N0", new CultureInfo("ar-EG")),
                FormattedBalance = 0m.ToString("N0", new CultureInfo("ar-EG")),
            }));

            transactions.AddRange(disbursements.Select(d => new TransactionDetail
            {
                TransactionDate = d.TransactionDate,
                PartyName = d.EntityName,
                TransactionType = "صرف",
                Notes = string.Empty,
                DisbursedAmountValue = d.Amount,
                FormattedReceivedAmount = 0m.ToString("N0", new CultureInfo("ar-EG")),
                FormattedDisbursedAmount = d.Amount.ToString("N0", new CultureInfo("ar-EG")),
                FormattedCredit = 0m.ToString("N0", new CultureInfo("ar-EG")),
                FormattedDebit = 0m.ToString("N0", new CultureInfo("ar-EG")),
                FormattedBalance = 0m.ToString("N0", new CultureInfo("ar-EG")),
            }));

            // Sort and assign
            Transactions = new ObservableCollection<TransactionDetail>(transactions.OrderBy(t => t.TransactionDate));
            _receivedAmount = transactions.Sum(t => t.ReceivedAmountValue);
            _totalReceivedCurrent = _receivedAmount;
            _totalDisbursedCurrent = transactions.Sum(t => t.DisbursedAmountValue);
            _totalRemainingAmount = _receivedAmount;
            _paidBackAmount = 0m;
            _remainingAmount = _receivedAmount;

            OnPropertyChanged(nameof(Transactions));
            OnPropertyChanged(nameof(FormattedTotalReceivedAll));
            OnPropertyChanged(nameof(FormattedTotalDisbursedAll));
            OnPropertyChanged(nameof(FormattedTreasuryNet));
            OnPropertyChanged(nameof(FormattedReceivedAmount));
            OnPropertyChanged(nameof(FormattedTotalRemainingAmount));
        }

        private static decimal TryParseDecimal(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0m;
            if (decimal.TryParse(s, NumberStyles.Any, new CultureInfo("ar-EG"), out var val)) return val;
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) return val;
            return 0m;
        }

        private static string SafeFormat(string? s)
        {
            var value = TryParseDecimal(s);
            return value.ToString("C0", new CultureInfo("ar-EG"));
        }

        public string EntityName => _accountStatement.EntityName;
        public decimal TotalCredit => _accountStatement.TotalCredit;
        public decimal TotalDebit => _accountStatement.TotalDebit;
        public decimal FinalBalance => Math.Abs(TotalCredit - TotalDebit);

        // New overall totals per requirements
        // Totals based on current filter (or all if none selected)
        private decimal _totalReceivedCurrent;
        private decimal _totalDisbursedCurrent;
        public decimal TotalReceivedCurrent => _totalReceivedCurrent;
        public decimal TotalDisbursedCurrent => _totalDisbursedCurrent;
        public decimal TreasuryNet => _totalReceivedCurrent - _totalDisbursedCurrent;

        public string FormattedTotalReceivedAll => _totalReceivedCurrent.ToString("N0", new CultureInfo("ar-EG"));
        public string FormattedTotalDisbursedAll => _totalDisbursedCurrent.ToString("N0", new CultureInfo("ar-EG"));
        public string FormattedTreasuryNet => TreasuryNet.ToString("N0", new CultureInfo("ar-EG"));
        public decimal PaidBackAmount => _paidBackAmount;
        public decimal ReceivedAmount => _receivedAmount;
        public decimal RemainingAmount => _remainingAmount;
        public decimal TotalRemainingAmount => _totalRemainingAmount;

        // Formatted properties for display
        public string FormattedTotalCredit => TotalCredit.ToString("C0", new CultureInfo("ar-EG"));
        public string FormattedTotalDebit => TotalDebit.ToString("C0", new CultureInfo("ar-EG"));
        public string FormattedFinalBalance => FinalBalance.ToString("C0", new CultureInfo("ar-EG"));
        public string FormattedPaidBackAmount => PaidBackAmount.ToString("C0", new CultureInfo("ar-EG"));
        public string FormattedReceivedAmount => ReceivedAmount.ToString("C0", new CultureInfo("ar-EG"));
        public string FormattedRemainingAmount => RemainingAmount.ToString("C0", new CultureInfo("ar-EG"));
        public string FormattedTotalRemainingAmount => TotalRemainingAmount.ToString("C0", new CultureInfo("ar-EG"));
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