using System.Windows.Controls;
using Fruittrack.Models;
using Fruittrack.ViewModels;

namespace Fruittrack
{
    /// <summary>
    /// Interaction logic for AccountStatementPage.xaml
    /// </summary>
    public partial class AccountStatementPage : Page
    {
        public AccountStatementPage(AccountStatement accountStatement)
        {
            InitializeComponent();
            DataContext = new AccountStatementViewModel(accountStatement);
        }
    }
} 