using System.Windows.Controls;
using Fruittrack.ViewModels;

namespace Fruittrack
{
    public partial class FactoryReportPage : Page
    {
        public FactoryReportPage()
        {
            InitializeComponent();
            var dbContext = ((App)App.Current).DbContext;
            DataContext = new FactoryReportViewModel(dbContext);
        }
    }
}



