using System.Windows;
using System.Windows.Controls;
using Fruittrack.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Fruittrack
{
    public partial class CashReceiptPage : Page
    {
        public CashReceiptPage()
        {
            InitializeComponent();

            try
            {
                var dbContext = App.ServiceProvider.GetRequiredService<FruitTrackDbContext>();
                this.DataContext = new CashReceiptPageViewModel(dbContext);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

       
    }
} 