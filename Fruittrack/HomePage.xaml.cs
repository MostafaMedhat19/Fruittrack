using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Fruittrack.Models;

namespace Fruittrack
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        private DispatcherTimer _timer;
        public int number = 0; 
        public HomePage()
        {
            InitializeComponent();
            this.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Montserrat");
            UpdateDateTime();
         
            // Start timer to update time every second
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateDateTime();
            _timer.Start();
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            DateTextBlock.Text = now.ToString("dddd, MMMM dd, yyyy", new System.Globalization.CultureInfo("ar-EG")); // Egypt uses Gregorian by default
            TimeTextBlock.Text = now.ToString("hh:mm tt", new System.Globalization.CultureInfo("en-US"));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to Supply Record Page
            NavigationService.Navigate(new SupplyRecordPage());
        }
     
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Navigate to Supplies Overview Page
            NavigationService.Navigate(new SuppliesOverview());
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // Navigate to Invalid Supplies Page
            NavigationService.Navigate(new InvalidSuppliesPage());
        }
       

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            // Navigate to Production Reports Page
            NavigationService.Navigate(new ProductionReportsPage());
        }

        private void Button_Click_CashReceipt(object sender, RoutedEventArgs e)
        {
            // Navigate to Cash Receipt Page
            NavigationService.Navigate(new CashReceiptPage());
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TotalProfitSummaryPage());
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CashDisbursementPage());
        }

        private void Button_Click_AccountStatement(object sender, RoutedEventArgs e)
        {
            var dbContext = ((App)Application.Current).DbContext;
            var statement = new AccountStatement { EntityName = string.Empty };
            NavigationService.Navigate(new AccountStatementPage(statement, dbContext));
        }

        private void Button_Click_FarmReport(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new FarmReportPage());
        }

        private void Button_Click_FactoryReport(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new FactoryReportPage());
        }
        private void LicenseInfoHyperlink_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new LicenseManagementPage());
        }

        private void SupportButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TechnicalSupportPage());
        }
    }
}