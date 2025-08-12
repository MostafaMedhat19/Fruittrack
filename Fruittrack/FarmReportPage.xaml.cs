using System.Windows;
using System.Windows.Controls;
using Fruittrack.ViewModels;
using Fruittrack.Utilities;

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

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            ExportUtilities.ExportToTemporaryPdfAndOpen(this, "كشف مزرعة ");
        }
        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("كشف_مزرعة.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportToPdf(this, filePath, "كشف مزرعة");
            }
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("كشف_مزرعة.xlsx", "Excel files (*.xlsx)|*.xlsx");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportDataGridToExcel(FarmDataGrid, filePath, "كشف مزرعة");
            }
        }
    }
}



