using System.Windows.Controls;
using Fruittrack.ViewModels;

namespace Fruittrack
{
    public partial class FarmReportPage : Page
    {
        public FarmReportPage()
        {
            InitializeComponent();
            var dbContext = ((App)App.Current).DbContext;
            DataContext = new FarmReportViewModel(dbContext);
        }
    }
}



