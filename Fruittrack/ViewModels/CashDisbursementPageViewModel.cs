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

            // Load transactions from database
            _transactionsViewSource = new CollectionViewSource();
            Transactions = new ObservableCollection<CashDisbursementTransaction>(
                _dbContext.CashDisbursementTransactions.OrderByDescending(t => t.TransactionDate).ToList());
            _transactionsViewSource.Source = Transactions;
            _transactionsViewSource.Filter += ApplyFilter;
            
            LoadEntityNames();
            
            // Initialize commands
            AddDisbursementCommand = new RelayCommand(AddDisbursement, CanAddDisbursement);
            DeleteTransactionCommand = new RelayCommand<CashDisbursementTransaction>(DeleteTransaction);
            ViewStatementCommand = new RelayCommand(ViewStatement, CanViewStatement);
            
            // Set default date
            TransactionDate = DateTime.Now;
        }

        private async void LoadEntityNames()
        {
            // Get unique entity names from both receipt and disbursement transactions
            var receiptNames = await _dbContext.CashReceiptTransactions
                .Select(t => t.SourceName)
                .Distinct()
                .ToListAsync();

            var disbursementNames = await _dbContext.CashDisbursementTransactions
                .Select(t => t.EntityName)
                .Distinct()
                .ToListAsync();

            var allNames = receiptNames.Union(disbursementNames).Distinct().OrderBy(n => n).ToList();
            EntityNames = new ObservableCollection<string>(allNames);
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

        // Form fields
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

        // DataGrid collection
        public ObservableCollection<CashDisbursementTransaction> Transactions { get; set; }

        // Selected transaction for editing
        private void ApplyFilter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                e.Accepted = true;
                return;
            }

            var transaction = e.Item as CashDisbursementTransaction;
            if (transaction == null) return;

            e.Accepted = transaction.EntityName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;
        }

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

        // Add new disbursement
        private void AddDisbursement(object obj)
        {
            try
            {
                var transaction = new CashDisbursementTransaction
                {
                    EntityName = EntityName,
                    Amount = Amount,
                    TransactionDate = TransactionDate,
                    Notes = Notes,
                    Debit = Amount, // Disbursement is a debit (عليه كام)
                    Credit = 0,     // No credit for disbursement
                    Balance = -Amount // Negative balance for disbursement
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

        // Delete transaction
        private void DeleteTransaction(CashDisbursementTransaction transaction)
        {
            if (transaction == null) return;

            var result = MessageBox.Show(
                $"هل انت متأكد من أنك تريد حذف معاملة {transaction.EntityName} بقيمة {transaction.Amount:C}؟",
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

        // View account statement
        private void ViewStatement(object obj)
        {
            if (SelectedTransaction == null)
            {
                MessageBox.Show("يرجى اختيار معاملة لعرض كشف الحساب", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var entityName = SelectedTransaction.EntityName;
                var accountStatement = GenerateAccountStatement(entityName);
                
                // Open account statement page
                var statementPage = new AccountStatementPage(accountStatement);
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow is NavigationWindow navigationWindow)
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

            // Get all receipt transactions for this entity
            var receiptTransactions = _dbContext.CashReceiptTransactions
                .Where(t => t.SourceName == entityName)
                .OrderBy(t => t.Date)
                .ToList();

            // Get all disbursement transactions for this entity
            var disbursementTransactions = _dbContext.CashDisbursementTransactions
                .Where(t => t.EntityName == entityName)
                .OrderBy(t => t.TransactionDate)
                .ToList();

            decimal runningBalance = 0;

            // Add receipt transactions (credits)
            foreach (var receipt in receiptTransactions)
            {
                runningBalance += receipt.ReceivedAmount;
                statement.Transactions.Add(new TransactionDetail
                {
                    TransactionDate = receipt.Date,
                    Amount = receipt.ReceivedAmount,
                    TransactionType = "استلام",
                    Notes = "استلام نقدية",
                    RunningBalance = runningBalance
                });
                statement.TotalCredit += receipt.ReceivedAmount;
            }

            // Add disbursement transactions (debits)
            foreach (var disbursement in disbursementTransactions)
            {
                runningBalance -= disbursement.Amount;
                statement.Transactions.Add(new TransactionDetail
                {
                    TransactionDate = disbursement.TransactionDate,
                    Amount = disbursement.Amount,
                    TransactionType = "صرف",
                    Notes = disbursement.Notes ?? "صرف نقدية",
                    RunningBalance = runningBalance
                });
                statement.TotalDebit += disbursement.Amount;
            }

            statement.FinalBalance = runningBalance;
            return statement;
        }

        private bool CanAddDisbursement(object obj)
        {
            return !string.IsNullOrWhiteSpace(EntityName) &&
                   Amount > 0 &&
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
            TransactionDate = DateTime.Now;
            Notes = string.Empty;
        }

        // Calculated properties for summary
        public decimal TotalCredit => Transactions?.Sum(t => t.Credit) ?? 0;
        public decimal TotalDebit => Transactions?.Sum(t => t.Debit) ?? 0;
        public decimal NetBalance => TotalCredit - TotalDebit;
        public Brush NetBalanceColor => NetBalance >= 0 ? Brushes.Green : Brushes.Red;

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // IDataErrorInfo for validation
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
                    case nameof(Amount):
                        if (Amount <= 0) return "المبلغ يجب أن يكون أكبر من صفر";
                        break;
                    case nameof(TransactionDate):
                        if (TransactionDate == default) return "التاريخ مطلوب";
                        break;
                }
                return null;
            }
        }
    }
} 