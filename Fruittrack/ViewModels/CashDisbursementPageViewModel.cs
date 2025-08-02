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
        private CollectionViewSource _transactionsViewSource = new();

        public CashDisbursementPageViewModel(FruitTrackDbContext dbContext)
        {
            _dbContext = dbContext;
            _transactionsViewSource = new CollectionViewSource();

            // Load transactions
            Transactions = new ObservableCollection<CashDisbursementTransaction>(
                _dbContext.CashDisbursementTransactions
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList());

            _transactionsViewSource.Source = Transactions;
            _transactionsViewSource.Filter += ApplyFilter;

            LoadEntityNames();

            // Initialize commands
            AddDisbursementCommand = new RelayCommand(AddDisbursement, CanAddDisbursement);
            DeleteTransactionCommand = new RelayCommand<CashDisbursementTransaction>(DeleteTransaction);
            ViewStatementCommand = new RelayCommand(ViewStatement, CanViewStatement);

            TransactionDate = DateTime.Now;
        }

        private async void LoadEntityNames()
        {
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
                if (value > 0) Debit = 0;
                OnPropertyChanged(nameof(Credit));
                OnPropertyChanged(nameof(Debit));
            }
        }

        private decimal _debit;
        public decimal Debit
        {
            get => _debit;
            set
            {
                _debit = value;
                if (value > 0) Credit = 0;
                OnPropertyChanged(nameof(Debit));
                OnPropertyChanged(nameof(Credit));
            }
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
                _transactionsViewSource.View.Refresh();
            }
        }

        public ICollectionView FilteredTransactions => _transactionsViewSource.View;
        public ObservableCollection<CashDisbursementTransaction> Transactions { get; set; }

        public CashDisbursementTransaction? SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                _selectedTransaction = value;
                OnPropertyChanged(nameof(SelectedTransaction));
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
                e.Accepted = transaction.EntityName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                              transaction.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;
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
                    Amount = Credit > 0 ? Credit : Debit,
                    Credit = Credit,
                    Debit = Debit,
                    Balance = CalculateNetBalance(EntityName)
                };

                _dbContext.CashDisbursementTransactions.Add(transaction);
                _dbContext.SaveChanges();

                Transactions.Insert(0, transaction);
                ClearForm();

                MessageBox.Show("تم حفظ عملية الصرف بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ العملية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal CalculateNetBalance(string entityName)
        {
            var totalCredit = _dbContext.CashReceiptTransactions
                .Where(t => t.SourceName == entityName)
                .Sum(t => (decimal?)t.ReceivedAmount) ?? 0;

            var totalDebit = _dbContext.CashDisbursementTransactions
                .Where(t => t.EntityName == entityName)
                .Sum(t => (decimal?)t.Debit) ?? 0;

            return totalCredit - totalDebit;
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
            decimal runningBalance = 0;

            // Process receipts (credits)
            var receipts = _dbContext.CashReceiptTransactions
                .Where(t => t.SourceName == entityName)
                .OrderBy(t => t.Date)
                .ToList();

            foreach (var receipt in receipts)
            {
                runningBalance += receipt.ReceivedAmount;
                statement.Transactions.Add(new TransactionDetail
                {
                    TransactionDate = receipt.Date,
                    TransactionType = "استلام",
                    Credit = receipt.ReceivedAmount,
                    Debit = 0,
                    Balance = runningBalance,
                    Notes =  "استلام نقدية"
                });
                statement.TotalCredit += receipt.ReceivedAmount;
            }

            // Process disbursements (debits)
            var disbursements = _dbContext.CashDisbursementTransactions
                .Where(t => t.EntityName == entityName)
                .OrderBy(t => t.TransactionDate)
                .ToList();

            foreach (var disbursement in disbursements)
            {
                runningBalance -= disbursement.Debit;
                statement.Transactions.Add(new TransactionDetail
                {
                    TransactionDate = disbursement.TransactionDate,
                    TransactionType = "صرف",
                    Credit = 0,
                    Debit = disbursement.Debit,
                    Balance = runningBalance,
                    Notes = disbursement.Notes ?? "صرف نقدية"
                });
                statement.TotalDebit += disbursement.Debit;
            }

            statement.FinalBalance = runningBalance;
            return statement;
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
        public decimal NetBalance => TotalCredit - TotalDebit;
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