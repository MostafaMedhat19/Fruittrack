using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Fruittrack.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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

            // Initialize commands
            AddDisbursementCommand = new RelayCommand(AddDisbursement, CanAddDisbursement);
            DeleteTransactionCommand = new RelayCommand<CashDisbursementTransaction>(DeleteTransaction);

            TransactionDate = DateTime.Now;

            // Run initialization sequentially to avoid concurrent DbContext operations
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadEntityNamesAsync();
            await LoadTransactionsAsync();
        }

        private async Task LoadTransactionsAsync()
        {
            try
            {
                // Load transactions
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
                OnPropertyChanged(nameof(FilteredTransactions));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading transactions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadEntityNamesAsync()
        {
            try
            {
                // Load entity names from both receipt and disbursement transactions
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
            set 
            { 
                _amount = value; 
                OnPropertyChanged(nameof(Amount)); 
            }
        }

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        private DateTime _transactionDate = DateTime.Now;
        public DateTime TransactionDate
        {
            get => _transactionDate;
            set { _transactionDate = value; OnPropertyChanged(nameof(TransactionDate)); }
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
            }
        }

        // Commands
        public ICommand AddDisbursementCommand { get; }
        public ICommand DeleteTransactionCommand { get; }

        private void ApplyFilter(object sender, FilterEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                e.Accepted = true;
                return;
            }

            if (e.Item is CashDisbursementTransaction transaction)
            {
                e.Accepted = transaction.EntityName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;
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
                    Amount = Amount,
                    Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim()
                };

                _dbContext.CashDisbursementTransactions.Add(transaction);
                _dbContext.SaveChanges();

                Transactions.Insert(0, transaction);
                ClearForm();

                // Refresh the view
                _transactionsViewSource.View.Refresh();
                OnPropertyChanged(nameof(FilteredTransactions));

                MessageBox.Show("تم حفظ عملية الصرف بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء حفظ العملية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

                    // Refresh the view
                    _transactionsViewSource.View.Refresh();
                    OnPropertyChanged(nameof(FilteredTransactions));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"حدث خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanAddDisbursement(object obj)
        {
            return !string.IsNullOrWhiteSpace(EntityName) &&
                   Amount > 0 &&
                   TransactionDate != default;
        }

        private void ClearForm()
        {
            EntityName = string.Empty;
            SelectedEntityName = string.Empty;
            Amount = 0;
            TransactionDate = DateTime.Now;
            Notes = string.Empty;
        }

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
                    case nameof(Amount):
                        if (Amount <= 0) return "المبلغ يجب أن يكون أكبر من صفر";
                        break;
                    case nameof(TransactionDate):
                        if (TransactionDate == default) return "التاريخ مطلوب";
                        break;
                    case nameof(Notes):
                        // No validation required for notes; can be empty
                        break;
                }
                return string.Empty;
            }
        }
    }
}