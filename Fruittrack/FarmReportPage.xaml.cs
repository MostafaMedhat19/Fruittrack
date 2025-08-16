using System.Windows;
using System.Windows.Controls;
using Fruittrack.ViewModels;
using Fruittrack.Utilities;
using System.Collections.Generic; // Added for Dictionary

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
            var viewModel = DataContext as FarmReportViewModel;
            if (viewModel != null)
            {
                var summaryData = new Dictionary<string, string>
                {
                   
                    { "إجمالي حساب المزرعة", viewModel.FormattedTotalFarmAmount },
                    { "إجمالي المصروف", viewModel.FormattedTotalDisbursed },
                    { "صافي المبلغ", viewModel.FormattedNetAmount }
                };

                ExportUtilities.ExportToTemporaryPdfWithSummaryAndOpen(this, "كشف مزرعة", summaryData: summaryData);
            }
            else
            {
                ExportUtilities.ExportToTemporaryPdfAndOpen(this, "كشف مزرعة");
            }
        }
        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("كشف_مزرعة.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                var viewModel = DataContext as FarmReportViewModel;
                if (viewModel != null)
                {
                    var summaryData = new Dictionary<string, string>
                    {
                        { "إجمالي الوزن", viewModel.FormattedTotalWeight },
                        { "إجمالي الوزن المسموح", viewModel.FormattedTotalAllowedWeight },
                        { "إجمالي مبلغ المزرعة", viewModel.FormattedTotalFarmAmount },
                        { "إجمالي المصروف", viewModel.FormattedTotalDisbursed },
                        { "صافي المبلغ", viewModel.FormattedNetAmount }
                    };

                    ExportUtilities.ExportToPdfWithSummary(this, filePath, "كشف مزرعة", summaryData: summaryData);
                }
                else
                {
                    ExportUtilities.ExportToPdf(this, filePath, "كشف مزرعة");
                }
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



