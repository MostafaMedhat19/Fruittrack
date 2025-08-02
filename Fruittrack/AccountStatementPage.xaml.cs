using System.Windows.Controls;
using Fruittrack.Models;
using Fruittrack.ViewModels;
using Fruittrack.Utilities;

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
            ExportUtilities.PrintPage(this, "كشف حساب");
        }

        private void PdfButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("كشف_حساب.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportToPdf(this, filePath, "كشف حساب");
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