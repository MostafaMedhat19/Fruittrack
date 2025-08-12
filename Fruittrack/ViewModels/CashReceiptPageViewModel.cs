using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

            // Load transactions from database
            _transactionsViewSource = new CollectionViewSource();
            Transactions = new ObservableCollection<CashReceiptTransaction>(
                _dbContext.CashReceiptTransactions.OrderByDescending(t => t.Date).ToList());
            _transactionsViewSource.Source = Transactions;
            _transactionsViewSource.Filter += ApplyFilter;
            LoadSourceNames();
            
            // Initialize commands
            AddTransactionCommand = new RelayCommand(AddTransaction, CanAddTransaction);
            UpdateTransactionCommand = new RelayCommand(UpdateTransaction, CanUpdateTransaction);
            EditTransactionCommand = new RelayCommand<CashReceiptTransaction>(EditTransaction);
            DeleteTransactionCommand = new RelayCommand<CashReceiptTransaction>(DeleteTransaction);
        }

        private async void LoadSourceNames()
        {
            // تجيب الأسماء الفريدة من قاعدة البيانات
            var names = await _dbContext.CashReceiptTransactions
                .Select(t => t.SourceName)
                .Distinct()
                .ToListAsync();

            SourceNames = new ObservableCollection<string>(names);
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

        // Form fields
        private string _sourceName = string.Empty;
        public string SourceName
        {
            get => _sourceName;
            set { _sourceName = value; OnPropertyChanged(nameof(SourceName)); }
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
                _transactionsViewSource.View.Refresh(); // Refresh filter when search text changes
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
        
        // DataGrid collection
        public ObservableCollection<CashReceiptTransaction> Transactions { get; set; }

        // Selected transaction for editing
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

        // Apply search filter only
        private void ApplyFilter(object sender, FilterEventArgs e)
        {
            var transaction = e.Item as CashReceiptTransaction;
            if (transaction == null) return;

            // Apply search filter only
            bool searchFilter = string.IsNullOrWhiteSpace(SearchText) ||
                               transaction.SourceName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                               transaction.Notes?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

            e.Accepted = searchFilter;
        }

        // Add new transaction
        private void AddTransaction(object obj)
        {
            var transaction = new CashReceiptTransaction
            {
                SourceName = SourceName,
                ReceivedAmount = ReceivedAmount,
                Date = Date,
                Notes = Notes
            };

            _dbContext.CashReceiptTransactions.Add(transaction);
            _dbContext.SaveChanges();

            Transactions.Insert(0, transaction); // Add at beginning of list
            _transactionsViewSource.View.Refresh();
            OnPropertyChanged(nameof(TotalReceivedAmount));
            ClearForm();

            MessageBox.Show("تم إضافة المعاملة بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Update existing transaction
        private void UpdateTransaction(object obj)
        {
            if (SelectedTransaction == null) return;

            // تحديث البيانات
            SelectedTransaction.SourceName = SourceName;
            SelectedTransaction.ReceivedAmount = ReceivedAmount;
            SelectedTransaction.Date = Date;
            SelectedTransaction.Notes = Notes;

            _dbContext.Entry(SelectedTransaction).State = EntityState.Modified;
            _dbContext.SaveChanges();

            // إعادة تحميل البيانات من قاعدة البيانات
            LoadTransactions();
            OnPropertyChanged(nameof(TotalReceivedAmount));
            
            // رسالة نجاح
            MessageBox.Show("تم تحديث البيانات", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

            // تفريغ النموذج
            ClearForm();
            SelectedTransaction = null;
        }

        private void LoadTransactions()
        {
            Transactions.Clear();
            foreach (var transaction in _dbContext.CashReceiptTransactions.OrderByDescending(t => t.Date).ToList())
            {
                Transactions.Add(transaction);
            }
            _transactionsViewSource.View.Refresh();
            OnPropertyChanged(nameof(TotalReceivedAmount));
        }

        // Edit transaction (populate form)
        private void EditTransaction(CashReceiptTransaction transaction)
        {
            SelectedTransaction = transaction;
        }

        //  deleting transactions
        private void DeleteTransaction(CashReceiptTransaction transaction)
        {
            if (transaction == null) return;

            // Show confirmation dialog
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
                    _dbContext.SaveChanges();
                    Transactions.Remove(transaction);
                    _transactionsViewSource.View.Refresh();
                    OnPropertyChanged(nameof(TotalReceivedAmount));
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