using System.Windows;
using System.Windows.Controls;
using Fruittrack.ViewModels;
using Fruittrack.Utilities;

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

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            ExportUtilities.ExportToTemporaryPdfAndOpen(this, "كشف مصنع ");
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("كشف_مصنع.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportToPdf(this, filePath, "كشف مصنع");
            }
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("كشف_مصنع.xlsx", "Excel files (*.xlsx)|*.xlsx");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportDataGridToExcel(FactoryDataGrid, filePath, "كشف مصنع");
            }
        }
    }
}



