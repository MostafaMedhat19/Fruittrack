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
        
        private async void SuppliesOverview_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            LoadFilterOptions();
        }
        
        private async Task LoadDataAsync()
        {
            try
            {
                // Load all supply entries with related data
                var supplies = await _context.SupplyEntries
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
                
                // Apply current filters
                ApplyFilters();
                
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
                
                // Update record count
                RecordCountText.Text = $"إجمالي السجلات: {_filteredSupplies.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تطبيق الفلاتر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    // Data model for the overview grid
    public class SupplyOverviewItem
    {
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
