using System.Windows.Controls;
using Fruittrack.ViewModels;
using Fruittrack.Utilities;

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

        private void PrintButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExportUtilities.PrintPage(this, "صرف نقدية");
        }

        private void PdfButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("صرف_نقدية.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportToPdf(this, filePath, "صرف نقدية");
            }
        }

     
    }
}
