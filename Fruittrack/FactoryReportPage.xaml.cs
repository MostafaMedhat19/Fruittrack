using System.Windows;
using System.Windows.Controls;
using Fruittrack.ViewModels;
using Fruittrack.Utilities;
using System.Collections.Generic; // Added for Dictionary

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
            var viewModel = DataContext as FactoryReportViewModel;
            if (viewModel != null)
            {
                var summaryData = new Dictionary<string, string>
                {
                   
                    { "إجمالي حساب المصنع", viewModel.FormattedTotalFactoryAmount },
                    { "إجمالي المستلم", viewModel.FormattedTotalReceived },
                    { "صافي المبلغ", viewModel.FormattedNetAmount }
                };

                ExportUtilities.ExportToTemporaryPdfWithSummaryAndOpen(this, "كشف مصنع", summaryData: summaryData);
            }
            else
            {
                ExportUtilities.ExportToTemporaryPdfAndOpen(this, "كشف مصنع");
            }
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("كشف_مصنع.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                var viewModel = DataContext as FactoryReportViewModel;
                if (viewModel != null)
                {
                    var summaryData = new Dictionary<string, string>
                    {
                        { "إجمالي الوزن", viewModel.FormattedTotalWeight },
                        { "إجمالي الوزن المسموح", viewModel.FormattedTotalAllowedWeight },
                        { "إجمالي مبلغ المصنع", viewModel.FormattedTotalFactoryAmount },
                        { "إجمالي المستلم", viewModel.FormattedTotalReceived },
                        { "صافي المبلغ", viewModel.FormattedNetAmount }
                    };

                    ExportUtilities.ExportToPdfWithSummary(this, filePath, "كشف مصنع", summaryData: summaryData);
                }
                else
                {
                    ExportUtilities.ExportToPdf(this, filePath, "كشف مصنع");
                }
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



