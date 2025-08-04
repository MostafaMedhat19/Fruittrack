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
        private string _searchText;
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

        private ObservableCollection<string> _sourceNames;
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
        private string _selectedFilter = "الكل";
        public string SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                _selectedFilter = value;
                OnPropertyChanged(nameof(SelectedFilter));
                _transactionsViewSource.View.Refresh(); // Refresh filter when selection changes
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
            }
        }
        public decimal TotalRemainingAmount
        {
            get => Transactions?.Sum(t => t.RemainingAmount) ?? 0;
        }

        public decimal RemainingAmount => ReceivedAmount - PaidBackAmount;
        public ICollectionView FilteredTransactions => _transactionsViewSource.View;
        // DataGrid collection
        public ObservableCollection<CashReceiptTransaction> Transactions { get; set; }

        // Selected transaction for editing
        // Add these properties to your ViewModel
       

        public ObservableCollection<string> FilterOptions { get; } = new ObservableCollection<string>
        {
            "الكل",
            "المعاملات المدفوعة",
            "المعاملات الغير مدفوعة"
        };

        // Modify the ApplyFilter method to include the new filter logic
        private void ApplyFilter(object sender, FilterEventArgs e)
        {
            var transaction = e.Item as CashReceiptTransaction;
            if (transaction == null) return;

            // Apply search filter
            bool searchFilter = string.IsNullOrWhiteSpace(SearchText) ||
                               transaction.SourceName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;

            // Apply payment status filter
            bool paymentFilter = true;
            switch (SelectedFilter)
            {
                case "المعاملات المدفوعة":
                    paymentFilter = transaction.RemainingAmount == 0;
                    break;
                case "المعاملات الغير مدفوعة":
                    paymentFilter = transaction.RemainingAmount != 0;
                    break;
                case "الكل":
                default:
                    paymentFilter = true;
                    break;
            }

            e.Accepted = searchFilter && paymentFilter;
        }
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
                    PaidBackAmount = value.PaidBackAmount;
                    Date = value.Date;
                }
            }
        }

        // Commands
        public ICommand AddTransactionCommand { get; }
        public ICommand UpdateTransactionCommand { get; }
        public ICommand EditTransactionCommand { get; }
        public ICommand DeleteTransactionCommand { get; }
        // Add new transaction
        private void AddTransaction(object obj)
        {
            // Validate that PaidBackAmount is less than ReceivedAmount
            if (PaidBackAmount >= ReceivedAmount)
            {
                MessageBox.Show("يجب أن يكون المبلغ المدفوع أقل من المبلغ المستلم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }
            if (PaidBackAmount < 0)
            {
                MessageBox.Show("لا يمكن أن يكون المبلغ المدفوع أقل من الصفر", "خطأ في التحقق", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            var transaction = new CashReceiptTransaction
            {
                SourceName = SourceName,
                ReceivedAmount = ReceivedAmount,
                Date = Date,
                PaidBackAmount = PaidBackAmount,
                RemainingAmount = RemainingAmount
            };

            _dbContext.CashReceiptTransactions.Add(transaction);

            _dbContext.SaveChanges();

            Transactions.Insert(0, transaction); // Add at beginning of list
            OnPropertyChanged(nameof(TotalRemainingAmount));
            ClearForm();
        }
        // Update existing transaction
        private void UpdateTransaction(object obj)
        {
            if (SelectedTransaction == null) return;

            // Validate that PaidBackAmount is less than ReceivedAmount
            if (PaidBackAmount >= ReceivedAmount)
            {
                MessageBox.Show("يجب أن يكون المبلغ المدفوع أقل من المبلغ المستلم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            // تحديث البيانات
            SelectedTransaction.SourceName = SourceName;
            SelectedTransaction.ReceivedAmount = ReceivedAmount;
            SelectedTransaction.PaidBackAmount = PaidBackAmount;
            SelectedTransaction.Date = Date;
            SelectedTransaction.RemainingAmount = RemainingAmount;

            _dbContext.Entry(SelectedTransaction).State = EntityState.Modified;
            _dbContext.SaveChanges();

            // إعادة تحميل البيانات من قاعدة البيانات
            LoadTransactions();
            OnPropertyChanged(nameof(TotalRemainingAmount));
            // رسالة نجاح
            MessageBox.Show("تم تحديث البيانات", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

            // تفريغ النموذج
            ClearForm();
            SelectedTransaction = null;
        }
        private void LoadTransactions()
        {
            Transactions.Clear();
            foreach (var transaction in _dbContext.CashReceiptTransactions.ToList())
            {
                Transactions.Add(transaction);
            }
            OnPropertyChanged(nameof(TotalRemainingAmount));
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
                    OnPropertyChanged(nameof(TotalRemainingAmount));
                    Transactions.Remove(transaction);
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
            PaidBackAmount = 0;
            Date = DateTime.Now;
            OnPropertyChanged(nameof(RemainingAmount));
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
                    case nameof(PaidBackAmount):
                        if (PaidBackAmount < 0) return "المبلغ المسدد لا يمكن أن يكون سالباً";
                        if (PaidBackAmount > ReceivedAmount) return "المبلغ المسدد لا يمكن أن يكون أكبر من المبلغ المستلم";
                        break;
                }
                return null;
            }
        }
    }
}