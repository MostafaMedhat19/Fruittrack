using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Fruittrack
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        private DispatcherTimer _timer;

        public HomePage()
        {
            InitializeComponent();
            this.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Montserrat");
            UpdateDateTime();
            
            // Start timer to update time every second
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) => UpdateDateTime();
            timer.Start();
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            DateTextBlock.Text = now.ToString("dddd, MMMM dd, yyyy", new System.Globalization.CultureInfo("ar-SA"));
            TimeTextBlock.Text = now.ToString("HH:mm tt");
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
    }
}
