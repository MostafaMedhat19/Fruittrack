using System.Windows;
using System.Windows.Controls;
using Fruittrack.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Fruittrack.Utilities;
using Fruittrack.Models;

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
            ExportUtilities.ExportToTemporaryPdfAndOpen(this, "صفحة استلام نقدية");
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
                try
                {
                    // Get the ViewModel to access the database context
                    var viewModel = this.DataContext as CashReceiptPageViewModel;
                    if (viewModel == null)
                    {
                        MessageBox.Show("خطأ في الوصول للبيانات", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Create DataTable with actual database columns
                    var dataTable = new System.Data.DataTable();
                    dataTable.Columns.Add("اسم الشخص", typeof(string));
                    dataTable.Columns.Add("المبلغ المستلم", typeof(decimal));
                    dataTable.Columns.Add("التاريخ", typeof(string));
                    dataTable.Columns.Add("المبلغ المسدد", typeof(decimal));
                    dataTable.Columns.Add("المتبقي", typeof(decimal));

                    // Get filtered data from database (respects current filters)
                    var filteredTransactions = viewModel.FilteredTransactions;
                    
                    // Check if there are any filters applied
                    bool hasFilters = !string.IsNullOrWhiteSpace(viewModel.SearchText) || 
                                    viewModel.SelectedFilter != "الكل";
                    
                    // Add filter information to the export
                    string sheetName = "صفحة استلام نقدية";
                    if (hasFilters)
                    {
                        sheetName += " (مفلترة)";
                    }

                    foreach (CashReceiptTransaction transaction in filteredTransactions)
                    {
                        var row = dataTable.NewRow();
                        row["اسم الشخص"] = transaction.SourceName;
                        row["المبلغ المستلم"] = transaction.ReceivedAmount;
                        row["التاريخ"] = transaction.Date.ToString("dd/MM/yyyy");
                        row["المبلغ المسدد"] = transaction.PaidBackAmount;
                        row["المتبقي"] = transaction.RemainingAmount;
                        dataTable.Rows.Add(row);
                    }

                    // Show summary of exported data
                    string summaryMessage = $"تم تصدير {dataTable.Rows.Count} معاملة";
                    if (hasFilters)
                    {
                        summaryMessage += $"\nمع تطبيق الفلاتر الحالية:";
                        if (!string.IsNullOrWhiteSpace(viewModel.SearchText))
                            summaryMessage += $"\n- البحث: {viewModel.SearchText}";
                        if (viewModel.SelectedFilter != "الكل")
                            summaryMessage += $"\n- نوع المعاملة: {viewModel.SelectedFilter}";
                    }
                    else
                    {
                        summaryMessage += "\n(جميع البيانات)";
                    }

                    ExportUtilities.ExportToExcel(dataTable, filePath, sheetName);
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تصدير البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
} 