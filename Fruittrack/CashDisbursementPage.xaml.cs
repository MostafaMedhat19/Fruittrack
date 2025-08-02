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

        private void ExcelButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("صرف_نقدية.xlsx", "Excel files (*.xlsx)|*.xlsx");
            if (!string.IsNullOrEmpty(filePath))
            {
                // For this page, we'll export the form data as a simple table
                var dataTable = new System.Data.DataTable();
                dataTable.Columns.Add("اسم الجهة", typeof(string));
                dataTable.Columns.Add("المبلغ المصروف", typeof(decimal));
                dataTable.Columns.Add("التاريخ", typeof(DateTime));
                dataTable.Columns.Add("الملاحظات", typeof(string));

                // Add sample data (you can modify this to get actual data from the view model)
                var row = dataTable.NewRow();
                row["اسم الجهة"] = "عينة";
                row["المبلغ المصروف"] = 0;
                row["التاريخ"] = DateTime.Now;
                row["الملاحظات"] = "عينة";
                dataTable.Rows.Add(row);

                ExportUtilities.ExportToExcel(dataTable, filePath, "صرف نقدية");
            }
        }
    }
}
