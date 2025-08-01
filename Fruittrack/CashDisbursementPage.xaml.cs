using System.Windows.Controls;
using Fruittrack.ViewModels;

namespace Fruittrack
{
    /// <summary>
    /// Interaction logic for CashDisbursementPage.xaml
    /// </summary>
    public partial class CashDisbursementPage : Page
    {
        public CashDisbursementPage()
        {
            InitializeComponent();
            
            // Get the DbContext from the application
            var dbContext = ((App)App.Current).DbContext;
            DataContext = new CashDisbursementPageViewModel(dbContext);
        }
    }
}
