using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Fruittrack.Models;

namespace Fruittrack.ViewModels
{
    public class AccountStatementViewModel : INotifyPropertyChanged
    {
        private readonly AccountStatement _accountStatement;

        public AccountStatementViewModel(AccountStatement accountStatement)
        {
            _accountStatement = accountStatement;
            GoBackCommand = new RelayCommand(GoBack);
        }

        public string EntityName => _accountStatement.EntityName;
        public decimal TotalCredit => _accountStatement.TotalCredit;
        public decimal TotalDebit => _accountStatement.TotalDebit;
        public decimal FinalBalance => _accountStatement.FinalBalance;
        public Brush FinalBalanceColor => FinalBalance >= 0 ? Brushes.Green : Brushes.Red;

        public ObservableCollection<TransactionDetail> Transactions => 
            new ObservableCollection<TransactionDetail>(_accountStatement.Transactions);

        public ICommand GoBackCommand { get; }

        private void GoBack(object obj)
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is NavigationWindow navigationWindow)
            {
                navigationWindow.GoBack();
            }
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
} 