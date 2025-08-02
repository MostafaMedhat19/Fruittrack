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
                FormattedCredit = t.Credit.ToString("C", new CultureInfo("ar-EG")),
                FormattedDebit = t.Debit.ToString("C", new CultureInfo("ar-EG")),
                FormattedBalance = t.Balance.ToString("C", new CultureInfo("ar-EG"))
            }));
        }

        public string EntityName => _accountStatement.EntityName;
        public decimal TotalCredit => _accountStatement.TotalCredit;
        public decimal TotalDebit => _accountStatement.TotalDebit;
        public decimal FinalBalance => _accountStatement.FinalBalance;

        // Formatted properties for display
        public string FormattedTotalCredit => TotalCredit.ToString("C", new CultureInfo("ar-EG"));
        public string FormattedTotalDebit => TotalDebit.ToString("C", new CultureInfo("ar-EG"));
        public string FormattedFinalBalance => FinalBalance.ToString("C", new CultureInfo("ar-EG"));

        public Brush FinalBalanceColor => FinalBalance >= 0 ? Brushes.Green : Brushes.Red;

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