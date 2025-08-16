using System.Windows.Controls;
using Fruittrack.Models;
using Fruittrack.ViewModels;
using Fruittrack.Utilities;
using System.Collections.Generic;
using System.Windows;

namespace Fruittrack
{
    /// <summary>
    /// Interaction logic for AccountStatementPage.xaml
    /// </summary>
    public partial class AccountStatementPage : Page
    {
        public AccountStatementPage(AccountStatement accountStatement, FruitTrackDbContext dbContext)
        {
            InitializeComponent();
            DataContext = new AccountStatementViewModel(accountStatement, dbContext);
        }

        private void PrintButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var viewModel = DataContext as AccountStatementViewModel;
            if (viewModel != null)
            {
                var summaryData = new Dictionary<string, string>
                {
                    { "إجمالي المستلم", viewModel.FormattedTotalReceivedAll },
                    { "إجمالي المصروف", viewModel.FormattedTotalDisbursedAll },
                    { "صافي الخزينة", viewModel.FormattedTreasuryNet },
                 
                };

                ExportUtilities.ExportToTemporaryPdfWithSummaryAndOpen(this, "كشف حساب", summaryData: summaryData);
            }
            else
            {
                ExportUtilities.ExportToTemporaryPdfAndOpen(this, "كشف حساب");
            }
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("كشف_حساب.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                var viewModel = DataContext as AccountStatementViewModel;
                if (viewModel != null)
                {
                    var summaryData = new Dictionary<string, string>
                    {
                        { "إجمالي المستلم", viewModel.FormattedTotalReceivedAll },
                        { "إجمالي المصروف", viewModel.FormattedTotalDisbursedAll },
                        { "صافي الخزينة", viewModel.FormattedTreasuryNet },
                      
                    };

                    ExportUtilities.ExportToPdfWithSummary(this, filePath, "كشف حساب", summaryData: summaryData);
                }
                else
                {
                    ExportUtilities.ExportToPdf(this, filePath, "كشف حساب");
                }
            }
        }

        private void ExcelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("كشف_حساب.xlsx", "Excel files (*.xlsx)|*.xlsx");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportDataGridToExcel(TransactionsDataGrid, filePath, "كشف حساب");
            }
        }

       
    }
} 