using System.Windows;
using System.Windows.Controls;
using Fruittrack.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Fruittrack.Utilities;

namespace Fruittrack
{
    public partial class CashReceiptPage : Page
    {
        public CashReceiptPage()
        {
            InitializeComponent();

            try
            {
                var dbContext = App.ServiceProvider.GetRequiredService<FruitTrackDbContext>();
                this.DataContext = new CashReceiptPageViewModel(dbContext);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            ExportUtilities.PrintPage(this, "صفحة استلام نقدية");
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("صفحة_استلام_نقدية.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportToPdf(this, filePath, "صفحة استلام نقدية");
            }
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("صفحة_استلام_نقدية.xlsx", "Excel files (*.xlsx)|*.xlsx");
            if (!string.IsNullOrEmpty(filePath))
            {
                // For this page, we'll export the form data as a simple table
                var dataTable = new System.Data.DataTable();
                dataTable.Columns.Add("اسم الشخص", typeof(string));
                dataTable.Columns.Add("المبلغ المستلم", typeof(decimal));
                dataTable.Columns.Add("التاريخ", typeof(DateTime));
                dataTable.Columns.Add("الملاحظات", typeof(string));

                // Add sample data (you can modify this to get actual data from the view model)
                var row = dataTable.NewRow();
                row["اسم الشخص"] = "عينة";
                row["المبلغ المستلم"] = 0;
                row["التاريخ"] = DateTime.Now;
                row["الملاحظات"] = "عينة";
                dataTable.Rows.Add(row);

                ExportUtilities.ExportToExcel(dataTable, filePath, "صفحة استلام نقدية");
            }
        }
    }
} 