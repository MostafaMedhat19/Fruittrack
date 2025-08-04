using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Fruittrack.Models;
using Microsoft.EntityFrameworkCore;

namespace Fruittrack.ViewModels
{
    public class CashDisbursementPageViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly FruitTrackDbContext _dbContext;
        private CashDisbursementTransaction? _selectedTransaction;
        private string _searchText = string.Empty;
        private CollectionViewSource _transactionsViewSource;

        public CashDisbursementPageViewModel(FruitTrackDbContext dbContext)
        {
            _dbContext = dbContext;
            _transactionsViewSource = new CollectionViewSource();

            // Initialize Transactions collection first
            Transactions = new ObservableCollection<CashDisbursementTransaction>();

            // Set the source for CollectionViewSource
            _transactionsViewSource.Source = Transactions;
            _transactionsViewSource.Filter += ApplyFilter;

            // Load data
            LoadTransactions();
            //LoadEntityNames();

            // Initialize commands
            AddDisbursementCommand = new RelayCommand(AddDisbursement, CanAddDisbursement);
            DeleteTransactionCommand = new RelayCommand<CashDisbursementTransaction>(DeleteTransaction);
            ViewStatementCommand = new RelayCommand(ViewStatement, CanViewStatement);

            TransactionDate = DateTime.Now;
        }

        private async void LoadTransactions()
        {
            try
            {
                // First load entity names and await completion
                await LoadEntityNames();

                // Then load transactions
                var transactions = await _dbContext.CashDisbursementTransactions
                    .OrderByDescending(t => t.TransactionDate)
                    .ToListAsync();

                Transactions.Clear();
                foreach (var transaction in transactions)
                {
                    Transactions.Add(transaction);
                }

                // Refresh the view after loading

                _transactionsViewSource.View.Refresh();
                 OnPropertyChanged(nameof(TotalCredit));
                OnPropertyChanged(nameof(TotalDebit));
                OnPropertyChanged(nameof(NetBalance));
                OnPropertyChanged(nameof(NetBalanceColor));
                OnPropertyChanged(nameof(FilteredTransactions));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transactions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadEntityNames()
        {
            try
            {
                // Execute both queries separately and await them
                var receiptNames = await _dbContext.CashReceiptTransactions
                    .Select(t => t.SourceName)
                    .Distinct()
                    .ToListAsync();

                var disbursementNames = await _dbContext.CashDisbursementTransactions
                    .Select(t => t.EntityName)
                    .Distinct()
                    .ToListAsync();

                EntityNames = new ObservableCollection<string>(
                    receiptNames.Union(disbursementNames)
                        .Distinct()
                        .OrderBy(n => n)
                        .ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading entity names: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ObservableCollection<string> _entityNames = new();
        public ObservableCollection<string> EntityNames
        {
            get => _entityNames;
            set
            {
                _entityNames = value;
                OnPropertyChanged(nameof(EntityNames));
            }
        }

        // Form properties
        private string _entityName = string.Empty;
        public string EntityName
        {
            get => _entityName;
            set
            {
                _entityName = value;
                OnPropertyChanged(nameof(EntityName));
                OnPropertyChanged(nameof(SelectedEntityName));
            }
        }

        private string _selectedEntityName = string.Empty;
        public string SelectedEntityName
        {
            get => _selectedEntityName;
            set
            {
                _selectedEntityName = value;
                EntityName = value;
                OnPropertyChanged(nameof(SelectedEntityName));
            }
        }

        private decimal _amount;
        public decimal Amount
        {
            get => _amount;
            set { _amount = value; OnPropertyChanged(nameof(Amount)); }
        }

        private decimal _credit;
        public decimal Credit
        {
            get => _credit;
            set
            {
                _credit = value;
                OnPropertyChanged(nameof(Credit));
                OnPropertyChanged(nameof(Balance));
                OnPropertyChanged(nameof(CanAddDisbursement));
            }
        }

        private decimal _debit;
        public decimal Debit
        {
            get => _debit;
            set
            {
                _debit = value;
                OnPropertyChanged(nameof(Debit));
                OnPropertyChanged(nameof(Balance));
                OnPropertyChanged(nameof(CanAddDisbursement));
            }
        }

        public decimal Balance
        {
            get => Math.Abs(Debit - Credit);
        }


        private DateTime _transactionDate = DateTime.Now;
        public DateTime TransactionDate
        {
            get => _transactionDate;
            set { _transactionDate = value; OnPropertyChanged(nameof(TransactionDate)); }
        }

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                _transactionsViewSource.View?.Refresh();
            }
        }

        public ICollectionView FilteredTransactions => _transactionsViewSource.View;
        public ObservableCollection<CashDisbursementTransaction> Transactions { get; }

        public CashDisbursementTransaction? SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                _selectedTransaction = value;
                OnPropertyChanged(nameof(SelectedTransaction));
                OnPropertyChanged(nameof(CanViewStatement));
            }
        }

        // Commands
        public ICommand AddDisbursementCommand { get; }
        public ICommand DeleteTransactionCommand { get; }
        public ICommand ViewStatementCommand { get; }

        private void ApplyFilter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                e.Accepted = true;
                return;
            }

            if (e.Item is CashDisbursementTransaction transaction)
            {
                e.Accepted =
                    (transaction.EntityName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (transaction.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
            }
        }

        private void AddDisbursement(object obj)
        {
            try
            {
                var transaction = new CashDisbursementTransaction
                {
                    EntityName = EntityName,
                    TransactionDate = TransactionDate,
                    Notes = Notes,
                    Amount = 0,
                    Credit = Credit,
                    Debit = Debit,
                  
                    
                };

                _dbContext.CashDisbursementTransactions.Add(transaction);
                _dbContext.SaveChanges();

                Transactions.Insert(0, transaction);
                ClearForm();

                // Refresh the view
                _transactionsViewSource.View.Refresh();
                OnPropertyChanged(nameof(FilteredTransactions));
                OnPropertyChanged(nameof(TotalCredit));
                OnPropertyChanged(nameof(TotalDebit));
                OnPropertyChanged(nameof(NetBalance));
                OnPropertyChanged(nameof(NetBalanceColor));

                MessageBox.Show("تم حفظ عملية الصرف بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ العملية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal CalculateNetBalance(string entityName)
        {
            var totalDebit = _dbContext.CashDisbursementTransactions
                .Where(t => t.EntityName == entityName)
                .Sum(t => (decimal?)t.Debit) ?? 0;

            var totalCredit = _dbContext.CashDisbursementTransactions
                .Where(t => t.EntityName == entityName)
                .Sum(t => (decimal?)t.Credit) ?? 0;

            var netBalance = Math.Abs(totalDebit - totalCredit);
            return netBalance;
        }

        private void DeleteTransaction(CashDisbursementTransaction transaction)
        {
            if (transaction == null) return;

            var result = MessageBox.Show(
                $"هل انت متأكد من أنك تريد حذف معاملة {transaction.EntityName} بقيمة {transaction.Debit:C}؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.CashDisbursementTransactions.Remove(transaction);
                    _dbContext.SaveChanges();
                    Transactions.Remove(transaction);

                    // Refresh the view and summary properties
                    _transactionsViewSource.View.Refresh();
                    OnPropertyChanged(nameof(FilteredTransactions));
                    OnPropertyChanged(nameof(TotalCredit));
                    OnPropertyChanged(nameof(TotalDebit));
                    OnPropertyChanged(nameof(NetBalance));
                    OnPropertyChanged(nameof(NetBalanceColor));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"حدث خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewStatement(object obj)
        {
            if (SelectedTransaction == null)
            {
                MessageBox.Show("يرجى اختيار معاملة لعرض كشف الحساب", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var accountStatement = GenerateAccountStatement(SelectedTransaction.EntityName);
                var statementPage = new AccountStatementPage(accountStatement, _dbContext);

                if (Application.Current.MainWindow is NavigationWindow navigationWindow)
                {
                    navigationWindow.Navigate(statementPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء فتح كشف الحساب: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private AccountStatement GenerateAccountStatement(string entityName)
        {
            var statement = new AccountStatement { EntityName = entityName };
       

            var allTransactions = new List<ITransaction>();

            var receipts = _dbContext.CashReceiptTransactions
                .Where(t => t.SourceName == entityName)
                .ToList();
            allTransactions.AddRange(receipts.Select(r => new ReceiptAdapter(r)));

            var disbursements = _dbContext.CashDisbursementTransactions
                .Where(t => t.EntityName == entityName)
                .ToList();
            allTransactions.AddRange(disbursements.Select(d => new DisbursementAdapter(d)));

            // Sort all transactions by date
            foreach (var transaction in allTransactions.OrderBy(t => t.GetDate()))
            {
                transaction.ProcessTransaction(statement);
            }

            statement.FinalBalance = Math.Abs(statement.TotalCredit - statement.TotalDebit);
            return statement;
        }

        // Helper interfaces and classes for unified processing
        private interface ITransaction
        {
            DateTime GetDate();
            bool ProcessTransaction(AccountStatement statement);
        }

        private class ReceiptAdapter : ITransaction
        {
            private readonly CashReceiptTransaction _receipt;
            public ReceiptAdapter(CashReceiptTransaction receipt) => _receipt = receipt;

            public DateTime GetDate() => _receipt.Date;

            public bool ProcessTransaction(AccountStatement statement)
            {
                decimal netAmount = Math.Abs(_receipt.ReceivedAmount - _receipt.PaidBackAmount);

                statement.Transactions.Add(new TransactionDetail
                {
                    TransactionDate = _receipt.Date,
                    TransactionType = "استلام",
                    FormattedPaidBackAmount = _receipt.PaidBackAmount.ToString(),
                    FormattedReceivedAmount = _receipt.ReceivedAmount.ToString(),
                    Notes = "استلام نقدية",
                    FormattedRemainingAmount = netAmount.ToString(),
                    Debit =0,
                    Balance = 0,
                    FormattedBalance = "0",
                    FormattedCredit = "0",
                    Credit = 0,
                    FormattedDebit = "0",


                });

                statement.TotalCredit += 0;
                statement.TotalDebit += 0;
                statement.FinalBalance = 0;
                return true;
            }
        }

        private class DisbursementAdapter : ITransaction
        {
            private readonly CashDisbursementTransaction _disbursement;
            public DisbursementAdapter(CashDisbursementTransaction disbursement) => _disbursement = disbursement;

            public DateTime GetDate() => _disbursement.TransactionDate;

            public bool ProcessTransaction(AccountStatement statement)
            {
                decimal netBalance = Math.Abs(_disbursement.Credit - _disbursement.Debit);

                statement.Transactions.Add(new TransactionDetail
                {
                    TransactionDate = _disbursement.TransactionDate,
                    TransactionType = "صرف",
                    Credit = _disbursement.Credit,
                    Debit = _disbursement.Debit,
                    Balance = netBalance,
                    Notes = _disbursement.Notes ?? "صرف نقدية",
                    FormattedDebit = _disbursement.Debit.ToString(),
                    FormattedCredit = _disbursement.Credit.ToString(),
                    FormattedBalance = netBalance.ToString(),
                    FormattedPaidBackAmount = "0",
                    FormattedReceivedAmount = "0",
                    FormattedRemainingAmount = "0"

                });

                statement.TotalDebit += _disbursement.Debit;
                statement.TotalCredit += _disbursement.Credit;
                statement.FinalBalance = Math.Abs(statement.TotalDebit - statement.TotalCredit);

                return true;
            }
        }

        private bool CanAddDisbursement(object obj)
        {
            return !string.IsNullOrWhiteSpace(EntityName) &&
                   (Credit > 0 || Debit > 0) &&
                   TransactionDate != default;
        }

        private bool CanViewStatement(object obj)
        {
            return SelectedTransaction != null;
        }

        private void ClearForm()
        {
            EntityName = string.Empty;
            SelectedEntityName = string.Empty;
            Amount = 0;
            Credit = 0;
            Debit = 0;
            TransactionDate = DateTime.Now;
            Notes = string.Empty;
        }

        // Summary properties
        public decimal TotalCredit => Transactions?.Sum(t => t.Credit) ?? 0;
        public decimal TotalDebit => Transactions?.Sum(t => t.Debit) ?? 0;
        public decimal NetBalance => Math.Abs(TotalCredit - TotalDebit);
        public Brush NetBalanceColor => NetBalance >= 0 ? Brushes.Green : Brushes.Red;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // IDataErrorInfo implementation
        public string Error => string.Empty;
        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(EntityName):
                        if (string.IsNullOrWhiteSpace(EntityName)) return "اسم الجهة مطلوب";
                        break;
                    case nameof(Credit):
                    case nameof(Debit):
                        if (Credit <= 0 && Debit <= 0) return "يجب إدخال مبلغ إما له أو عليه";
                        break;
                    case nameof(TransactionDate):
                        if (TransactionDate == default) return "التاريخ مطلوب";
                        break;
                }
                return string.Empty;
            }
        }
    }
}