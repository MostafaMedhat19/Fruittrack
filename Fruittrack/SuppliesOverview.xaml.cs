using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Fruittrack.Models;
using Fruittrack.Utilities;
using Fruittrack.Views;

namespace Fruittrack
{
    /// <summary>
    /// Interaction logic for SuppliesOverview.xaml
    /// </summary>
    public partial class SuppliesOverview : Page
    {
        private readonly FruitTrackDbContext _context;
        private ObservableCollection<SupplyOverviewItem> _allSupplies;
        private ObservableCollection<SupplyOverviewItem> _filteredSupplies;
        
        public SuppliesOverview()
        {
            InitializeComponent();
            
            // Get database context
            _context = App.ServiceProvider.GetRequiredService<FruitTrackDbContext>();
            
            // Initialize collections
            _allSupplies = new ObservableCollection<SupplyOverviewItem>();
            _filteredSupplies = new ObservableCollection<SupplyOverviewItem>();
            
            // Set DataGrid source
            SuppliesDataGrid.ItemsSource = _filteredSupplies;
            
            // Wire up events
            Loaded += SuppliesOverview_Loaded;
            ClearButton.Click += ClearButton_Click;
            
            // Real-time filtering on text change
            TruckNumberFilter.TextChanged += Filter_Changed;
            FarmFilter.SelectionChanged += Selection_Changed;
            FactoryFilter.SelectionChanged += Selection_Changed;
            ProfitTypeFilter.SelectionChanged += Selection_Changed;
            FromDatePicker.SelectedDateChanged += DateChanged;
            ToDatePicker.SelectedDateChanged += DateChanged;
            
            // Set default profit type to "الكل"
            ProfitTypeFilter.SelectedIndex = 0;
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            ExportUtilities.PrintPage(this, "نظرة عامة على كل التوريدات");
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("نظرة_عامة_التوريدات.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportToPdf(this, filePath, "نظرة عامة على كل التوريدات");
            }
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("نظرة_عامة_التوريدات.xlsx", "Excel files (*.xlsx)|*.xlsx");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportDataGridToExcel(SuppliesDataGrid, filePath, "نظرة عامة على كل التوريدات");
            }
        }
        
        private async void SuppliesOverview_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            LoadFilterOptions();
        }
        
        private async Task LoadDataAsync()
        {
            try
            {
                // Ensure we do not reuse stale tracked entities
                _context.ChangeTracker.Clear();

                // Load all supply entries with related data, always fresh from DB
                var supplies = await _context.SupplyEntries
                    .AsNoTracking()
                    .Include(s => s.Truck)
                    .Include(s => s.Farm)
                    .Include(s => s.Factory)
                    .OrderByDescending(s => s.EntryDate)
                    .ToListAsync();
                
                _allSupplies.Clear();
                
                foreach (var supply in supplies)
                {
                    var item = new SupplyOverviewItem
                    {
                        SupplyEntryId = supply.SupplyEntryId, // Include ID for edit/delete operations
                        Date = supply.EntryDate,
                        TruckNumber = supply.Truck?.TruckNumber ?? "غير محدد",
                        FreightCost = supply.FreightCost ?? 0,
                        
                        // Farm data
                        FarmName = supply.Farm?.FarmName ?? "غير محدد",
                        FarmWeight = supply.FarmWeight ?? 0,
                        FarmDiscountPercentage = supply.FarmDiscountRate ?? 0,
                        FarmPrice = supply.FarmPricePerKilo ?? 0,
                        
                        // Factory data  
                        FactoryName = supply.Factory?.FactoryName ?? "غير محدد",
                        FactoryWeight = supply.FactoryWeight ?? 0,
                        FactoryDiscountPercentage = supply.FactoryDiscountRate ?? 0,
                        FactoryPrice = supply.FactoryPricePerKilo ?? 0,
                        
                        // Notes
                        Notes = supply.Notes ?? ""
                    };
                    
                    // Calculate derived values
                    item.AllowedWeightFromFarm = item.FarmWeight * (1 - (item.FarmDiscountPercentage / 100));
                    item.FarmTotal = item.AllowedWeightFromFarm * item.FarmPrice;
                    
                    item.AllowedWeightFromFactory = item.FactoryWeight * (1 - (item.FactoryDiscountPercentage / 100));
                    item.FactoryTotal = item.AllowedWeightFromFactory * item.FactoryPrice;
                    
                    item.ProfitLoss = item.FactoryTotal - (item.FarmTotal + item.FreightCost);
                    
                    _allSupplies.Add(item);
                }
                
                // Apply current filters and refresh grid
                ApplyFilters();
                SuppliesDataGrid.Items.Refresh();
                
                // Update status
                LastUpdateText.Text = $"آخر تحديث: {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void LoadFilterOptions()
        {
            try
            {
                // Load unique farm names
                var farms = _context.Farms.Select(f => f.FarmName).Distinct().OrderBy(f => f).ToList();
                FarmFilter.Items.Clear();
                FarmFilter.Items.Add(""); // Empty option for "all"
                foreach (var farm in farms)
                {
                    FarmFilter.Items.Add(farm);
                }
                
                // Load unique factory names
                var factories = _context.Factories.Select(f => f.FactoryName).Distinct().OrderBy(f => f).ToList();
                FactoryFilter.Items.Clear();
                FactoryFilter.Items.Add(""); // Empty option for "all"
                foreach (var factory in factories)
                {
                    FactoryFilter.Items.Add(factory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل خيارات الفلتر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void Filter_Changed(object sender, EventArgs e)
        {
            ApplyFilters();
        }
        
        private void Selection_Changed(object? sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }
        
        private void DateChanged(object? sender, EventArgs e)
        {
            ApplyFilters();
        }
        
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear all filters
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            TruckNumberFilter.Text = "";
            FarmFilter.SelectedIndex = 0; // Select empty option
            FactoryFilter.SelectedIndex = 0; // Select empty option
            ProfitTypeFilter.SelectedIndex = 0; // Select "الكل"
            
            // Apply filters to show all data
            ApplyFilters();
        }
        
        private void ApplyFilters()
        {
            try
            {
                var filtered = _allSupplies.AsEnumerable();
                
                // Date range filter
                if (FromDatePicker.SelectedDate.HasValue)
                {
                    filtered = filtered.Where(s => s.Date >= FromDatePicker.SelectedDate.Value);
                }
                
                if (ToDatePicker.SelectedDate.HasValue)
                {
                    filtered = filtered.Where(s => s.Date <= ToDatePicker.SelectedDate.Value);
                }
                
                // Truck number filter
                if (!string.IsNullOrWhiteSpace(TruckNumberFilter.Text))
                {
                    string truckFilter = TruckNumberFilter.Text.Trim();
                    filtered = filtered.Where(s => s.TruckNumber.Contains(truckFilter, StringComparison.OrdinalIgnoreCase));
                }
                
                // Farm filter
                if (FarmFilter.SelectedItem != null && !string.IsNullOrWhiteSpace(FarmFilter.SelectedItem.ToString()))
                {
                    string farmFilter = FarmFilter.SelectedItem.ToString();
                    filtered = filtered.Where(s => s.FarmName.Equals(farmFilter, StringComparison.OrdinalIgnoreCase));
                }
                
                // Factory filter
                if (FactoryFilter.SelectedItem != null && !string.IsNullOrWhiteSpace(FactoryFilter.SelectedItem.ToString()))
                {
                    string factoryFilter = FactoryFilter.SelectedItem.ToString();
                    filtered = filtered.Where(s => s.FactoryName.Equals(factoryFilter, StringComparison.OrdinalIgnoreCase));
                }
                
                // Profit type filter
                if (ProfitTypeFilter.SelectedItem is ComboBoxItem selectedProfitItem)
                {
                    string profitType = selectedProfitItem.Content.ToString();
                    switch (profitType)
                    {
                        case "ربح":
                            filtered = filtered.Where(s => s.ProfitLoss > 0);
                            break;
                        case "خسارة":
                            filtered = filtered.Where(s => s.ProfitLoss < 0);
                            break;
                        case "صفر":
                            filtered = filtered.Where(s => Math.Abs(s.ProfitLoss) < 0.01m); // Nearly zero
                            break;
                        // "الكل" shows all records
                    }
                }
                
                // Update filtered collection
                _filteredSupplies.Clear();
                foreach (var item in filtered)
                {
                    _filteredSupplies.Add(item);
                }
                
                // Force DataGrid to show changes
                SuppliesDataGrid.Items.Refresh();
                
                // Update record count
                RecordCountText.Text = $"إجمالي السجلات: {_filteredSupplies.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تطبيق الفلاتر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is SupplyOverviewItem item)
                {
                    var editDialog = new EditSupplyEntryDialog(item.SupplyEntryId);
                    if (editDialog.ShowDialog() == true)
                    {
                        // Show loading indicator
                        SuppliesDataGrid.IsEnabled = false;
                        
                        // Refresh the data grid after successful edit
                        await LoadDataAsync();

                        // Try to focus the edited row if it matches current filters
                        FocusEditedRow(item.SupplyEntryId);
                        
                        // Re-enable grid
                        SuppliesDataGrid.IsEnabled = true;
                        
                        // Show success message
                        MessageBox.Show("تم تحديث البيانات بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                SuppliesDataGrid.IsEnabled = true;
                MessageBox.Show($"خطأ في فتح نافذة التعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FocusEditedRow(int supplyEntryId)
        {
            try
            {
                var edited = _filteredSupplies.FirstOrDefault(x => x.SupplyEntryId == supplyEntryId);
                if (edited != null)
                {
                    SuppliesDataGrid.SelectedItem = edited;
                    SuppliesDataGrid.UpdateLayout();
                    SuppliesDataGrid.ScrollIntoView(edited);
                }
                else
                {
                    // If not found in current filter, inform user once
                    MessageBox.Show("تم حفظ التغييرات، لكن قد لا يظهر السجل بسبب الفلاتر الحالية.", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch
            {
                // Ignore focus errors
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is SupplyOverviewItem item)
                {
                    // Show confirmation dialog
                    var result = MessageBox.Show(
                        $"هل أنت متأكد من حذف سجل التوريد بتاريخ {item.Date:dd/MM/yyyy} للعربية رقم {item.TruckNumber}؟\n\nلا يمكن التراجع عن هذا الإجراء.",
                        "تأكيد الحذف",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning,
                        MessageBoxResult.No);

                    if (result == MessageBoxResult.Yes)
                    {
                        await DeleteSupplyEntry(item.SupplyEntryId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حذف السجل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteSupplyEntry(int supplyEntryId)
        {
            try
            {
                var supplyEntry = await _context.SupplyEntries
                    .Include(s => s.FinancialSettlement)
                    .FirstOrDefaultAsync(s => s.SupplyEntryId == supplyEntryId);

                if (supplyEntry == null)
                {
                    MessageBox.Show("لم يتم العثور على السجل المطلوب حذفه", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Remove the supply entry (cascading delete will handle related financial settlement)
                _context.SupplyEntries.Remove(supplyEntry);
                await _context.SaveChangesAsync();

                MessageBox.Show("تم حذف السجل بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh the data grid
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حذف السجل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    // Data model for the overview grid
    public class SupplyOverviewItem
    {
        public int SupplyEntryId { get; set; } // Add ID for edit/delete operations
        public DateTime Date { get; set; }
        public string TruckNumber { get; set; } = "";
        public decimal FreightCost { get; set; }
        
        // Farm section
        public string FarmName { get; set; } = "";
        public decimal FarmWeight { get; set; }
        public decimal FarmDiscountPercentage { get; set; }
        public decimal AllowedWeightFromFarm { get; set; }
        public decimal FarmPrice { get; set; }
        public decimal FarmTotal { get; set; }
        
        // Factory section
        public string FactoryName { get; set; } = "";
        public decimal FactoryWeight { get; set; }
        public decimal FactoryDiscountPercentage { get; set; }
        public decimal AllowedWeightFromFactory { get; set; }
        public decimal FactoryPrice { get; set; }
        public decimal FactoryTotal { get; set; }
        
        // Profit/Loss
        public decimal ProfitLoss { get; set; }
        
        // Notes
        public string Notes { get; set; } = "";
    }
}
