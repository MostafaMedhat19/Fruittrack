using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Fruittrack.Models;
using Microsoft.EntityFrameworkCore;

namespace Fruittrack.ViewModels
{
    public class CashReceiptPageViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly FruitTrackDbContext _dbContext;
        private CashReceiptTransaction _selectedTransaction;
        private string _searchText = string.Empty;
        private CollectionViewSource _transactionsViewSource;

        public CashReceiptPageViewModel(FruitTrackDbContext dbContext)
        {
            _dbContext = dbContext;

            // Initialize collections and commands
            _transactionsViewSource = new CollectionViewSource();
            Transactions = new ObservableCollection<CashReceiptTransaction>();
            _transactionsViewSource.Source = Transactions;
            _transactionsViewSource.Filter += ApplyFilter;

            // Initialize commands
          AddTransactionCommand = new RelayCommand(async (param) => await AddTransaction(), CanAddTransaction);
          UpdateTransactionCommand = new RelayCommand(async (param) => await UpdateTransaction(), CanUpdateTransaction);
            EditTransactionCommand = new RelayCommand<CashReceiptTransaction>(EditTransaction);
            DeleteTransactionCommand = new RelayCommand<CashReceiptTransaction>(async (t) => await DeleteTransaction(t));

            // Load initial data
            _ = LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            await LoadTransactionsAsync();
            await LoadSourceNamesAsync();
        }

        private ObservableCollection<string> _sourceNames = new();
        public ObservableCollection<string> SourceNames
        {
            get => _sourceNames;
            set
            {
                _sourceNames = value;
                OnPropertyChanged(nameof(SourceNames));
            }
        }

        private string _sourceName = string.Empty;
        public string SourceName
        {
            get => _sourceName;
            set
            {
                if (_sourceName != value)
                {
                    _sourceName = value;
                    OnPropertyChanged(nameof(SourceName));

                    // Load suggestions when text changes
                    if (!string.IsNullOrWhiteSpace(value) && value.Length > 0)
                    {
                        _ = LoadSuggestedNamesAsync(value);
                    }
                    else
                    {
                        // If empty, load all unique names
                        _ = LoadSourceNamesAsync();
                    }
                }
            }
        }

        private async Task LoadSourceNamesAsync()
        {
            try
            {
                // Load factory names
                var factoryNames = await _dbContext.Factories
                    .AsNoTracking()
                    .Select(f => f.FactoryName)
                    .OrderBy(name => name)
                    .ToListAsync();

                // Also load existing transaction source names
                var transactionNames = await _dbContext.CashReceiptTransactions
                    .AsNoTracking()
                    .Select(t => t.SourceName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToListAsync();

                // Combine and remove duplicates
                var allNames = factoryNames.Union(transactionNames).OrderBy(name => name).ToList();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SourceNames = new ObservableCollection<string>(allNames);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading source names: {ex.Message}");
            }
        }

        private async Task LoadSuggestedNamesAsync(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SourceNames.Clear();
                    });
                    return;
                }

                // Search in factory names
                var factoryNames = await _dbContext.Factories
                    .AsNoTracking()
                    .Where(f => f.FactoryName != null && f.FactoryName.Contains(searchText))
                    .Select(f => f.FactoryName)
                    .OrderBy(name => name)
                    .ToListAsync();

                // Search in farm names
                var farmNames = await _dbContext.Farms
                    .AsNoTracking()
                    .Where(f => f.FarmName != null && f.FarmName.Contains(searchText))
                    .Select(f => f.FarmName)
                    .OrderBy(name => name)
                    .ToListAsync();

                // Search in transaction source names
                var transactionNames = await _dbContext.CashReceiptTransactions
                    .AsNoTracking()
                    .Where(t => t.SourceName != null && t.SourceName.Contains(searchText))
                    .Select(t => t.SourceName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToListAsync();

                // Combine results without duplicates
                var allNames = factoryNames
                    .Union(farmNames)
                    .Union(transactionNames)
                    .OrderBy(name => name)
                    .ToList();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    SourceNames = new ObservableCollection<string>(allNames);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading suggested names: {ex.Message}");
            }
        }

        private decimal _receivedAmount;
        public decimal ReceivedAmount
        {
            get => _receivedAmount;
            set { _receivedAmount = value; OnPropertyChanged(nameof(ReceivedAmount)); }
        }

        private DateTime _date = DateTime.Now;
        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(nameof(Date)); }
        }

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        private string _selectedSourceName = string.Empty;
        public string SelectedSourceName
        {
            get => _selectedSourceName;
            set
            {
                _selectedSourceName = value;
                SourceName = value;
                OnPropertyChanged(nameof(SelectedSourceName));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                _transactionsViewSource.View?.Refresh();
                OnPropertyChanged(nameof(TotalReceivedAmount));
            }
        }

        public decimal TotalReceivedAmount
        {
            get
            {
                var view = _transactionsViewSource?.View;
                if (view == null) return 0;

                decimal total = 0;
                foreach (var item in view)
                {
                    if (item is CashReceiptTransaction t)
                    {
                        total += t.ReceivedAmount;
                    }
                }
                return total;
            }
        }

        public ICollectionView FilteredTransactions => _transactionsViewSource.View;
        
        public ObservableCollection<CashReceiptTransaction> Transactions { get; set; }

        public CashReceiptTransaction SelectedTransaction
        {
            get => _selectedTransaction;
            set
            {
                _selectedTransaction = value;
                OnPropertyChanged(nameof(SelectedTransaction));

                if (value != null)
                {
                    // Populate form fields when transaction is selected
                    SourceName = value.SourceName;
                    ReceivedAmount = value.ReceivedAmount;
                    Date = value.Date;
                    Notes = value.Notes;
                }
            }
        }

        // Commands
        public ICommand AddTransactionCommand { get; }
        public ICommand UpdateTransactionCommand { get; }
        public ICommand EditTransactionCommand { get; }
        public ICommand DeleteTransactionCommand { get; }

        private void ApplyFilter(object sender, FilterEventArgs e)
        {
            var transaction = e.Item as CashReceiptTransaction;
            if (transaction == null) return;

            bool searchFilter = string.IsNullOrWhiteSpace(SearchText) ||
                               transaction.SourceName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                               transaction.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

            e.Accepted = searchFilter;
        }

        private async Task AddTransaction()
        {
            try
            {
                var transaction = new CashReceiptTransaction
                {
                    SourceName = SourceName,
                    ReceivedAmount = ReceivedAmount,
                    Date = Date,
                    Notes = Notes
                };

                await _dbContext.CashReceiptTransactions.AddAsync(transaction);
                await _dbContext.SaveChangesAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Transactions.Insert(0, transaction);
                    _transactionsViewSource.View.Refresh();
                    OnPropertyChanged(nameof(TotalReceivedAmount));
                    ClearForm();
                });

                MessageBox.Show("تم إضافة المعاملة بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الإضافة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateTransaction()
        {
            if (SelectedTransaction == null) return;

            try
            {
                SelectedTransaction.SourceName = SourceName;
                SelectedTransaction.ReceivedAmount = ReceivedAmount;
                SelectedTransaction.Date = Date;
                SelectedTransaction.Notes = Notes;

                _dbContext.Entry(SelectedTransaction).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();

                await LoadTransactionsAsync();
                OnPropertyChanged(nameof(TotalReceivedAmount));
                
                MessageBox.Show("تم تحديث البيانات", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                SelectedTransaction = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التحديث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadTransactionsAsync()
        {
            try
            {
                var transactions = await _dbContext.CashReceiptTransactions
                    .AsNoTracking()
                    .OrderByDescending(t => t.Date)
                    .ToListAsync();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    Transactions.Clear();
                    foreach (var transaction in transactions)
                    {
                        Transactions.Add(transaction);
                    }
                    _transactionsViewSource.View.Refresh();
                    OnPropertyChanged(nameof(TotalReceivedAmount));
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading transactions: {ex.Message}");
            }
        }

        private void EditTransaction(CashReceiptTransaction transaction)
        {
            SelectedTransaction = transaction;
        }

        private async Task DeleteTransaction(CashReceiptTransaction transaction)
        {
            if (transaction == null) return;

            var result = MessageBox.Show(
                $"هل انت متأكد من أنك تريد حذف معاملة {transaction.SourceName} بقيمة {transaction.ReceivedAmount}؟",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.CashReceiptTransactions.Remove(transaction);
                    await _dbContext.SaveChangesAsync();

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Transactions.Remove(transaction);
                        _transactionsViewSource.View.Refresh();
                        OnPropertyChanged(nameof(TotalReceivedAmount));
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"حدث خطأ أثناء الحذف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanAddTransaction(object obj)
        {
            return !string.IsNullOrWhiteSpace(SourceName) &&
                   ReceivedAmount > 0 &&
                   Date != default;
        }

        private bool CanUpdateTransaction(object obj)
        {
            return SelectedTransaction != null &&
                   !string.IsNullOrWhiteSpace(SourceName) &&
                   ReceivedAmount > 0 &&
                   Date != default;
        }

        private void ClearForm()
        {
            SourceName = string.Empty;
            ReceivedAmount = 0;
            Date = DateTime.Now;
            Notes = string.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Error => null;
        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(SourceName):
                        if (string.IsNullOrWhiteSpace(SourceName)) return "اسم الجهة مطلوب";
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
