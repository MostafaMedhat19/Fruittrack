using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.Generic; // Added missing import for List
using Fruittrack.Utilities;

namespace Fruittrack
{
    public partial class TotalProfitSummaryPage : Page, INotifyPropertyChanged
    {
        private readonly FruitTrackDbContext _context;
        private ObservableCollection<SummaryItem> _allSummaries;
        private ObservableCollection<SummaryItem> _filteredSummaries;

        public ObservableCollection<SummaryItem> AllSummaries
        {
            get => _allSummaries;
            set
            {
                _allSummaries = value;
                OnPropertyChanged(nameof(AllSummaries));
            }
        }

        public ObservableCollection<SummaryItem> FilteredSummaries
        {
            get => _filteredSummaries;
            set
            {
                _filteredSummaries = value;
                OnPropertyChanged(nameof(FilteredSummaries));
                RecordCountText.Text = $"{_filteredSummaries?.Count ?? 0} سجل";
            }
        }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public TotalProfitSummaryPage()
        {
            InitializeComponent();
            _context = App.ServiceProvider.GetRequiredService<FruitTrackDbContext>();
            
            AllSummaries = new ObservableCollection<SummaryItem>();
            FilteredSummaries = new ObservableCollection<SummaryItem>();
            
            DataContext = this;
            
            // Set default date range (last 30 days)
            ToDate = DateTime.Today;
            FromDate = DateTime.Today.AddDays(-30);
            FromDatePicker.SelectedDate = FromDate;
            ToDatePicker.SelectedDate = ToDate;
            
            LoadFilters();
            LoadData();

            // Live filtering for truck number typing
            TruckNumberFilter.TextChanged += (_, __) => ApplyFilters();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            ExportUtilities.ExportToTemporaryPdfAndOpen(this, "ملخص الربح الإجمالي");
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("ملخص_الربح_الإجمالي.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportToPdf(this, filePath, "ملخص الربح الإجمالي");
            }
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("ملخص_الربح_الإجمالي.xlsx", "Excel files (*.xlsx)|*.xlsx");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportDataGridToExcel(SummaryDataGrid, filePath, "ملخص الربح الإجمالي");
            }
        }

        private void LoadFilters()
        {
            try
            {
                // Load Farm names
                var farms = _context.SupplyEntries
                    .Where(s => s.Farm != null)
                    .Select(s => s.Farm.FarmName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                FarmFilter.Items.Clear();
                FarmFilter.Items.Add("الكل");
                foreach (var farm in farms)
                {
                    FarmFilter.Items.Add(farm);
                }
                FarmFilter.SelectedIndex = 0;

                // Load Factory names
                var factories = _context.SupplyEntries
                    .Where(s => s.Factory != null)
                    .Select(s => s.Factory.FactoryName)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                FactoryFilter.Items.Clear();
                FactoryFilter.Items.Add("الكل");
                foreach (var factory in factories)
                {
                    FactoryFilter.Items.Add(factory);
                }
                FactoryFilter.SelectedIndex = 0;

                StatusText.Text = "تم تحميل الفلاتر بنجاح";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"خطأ في تحميل الفلاتر: {ex.Message}";
                MessageBox.Show($"حدث خطأ أثناء تحميل الفلاتر:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                StatusText.Text = "جاري تحميل البيانات...";

                var supplies = _context.SupplyEntries
                    .Include(s => s.Farm)
                    .Include(s => s.Factory)
                    .Include(s => s.Truck)
                    .AsNoTracking()
                    .ToList();

                AllSummaries.Clear();

                // Group supplies by date, farm, factory, and truck
                var groupedSupplies = supplies
                    .GroupBy(s => new
                    {
                        Date = s.EntryDate.Date,
                        FarmName = s.Farm?.FarmName ?? "غير محدد",
                        FactoryName = s.Factory?.FactoryName ?? "غير محدد",
                        TruckNumber = s.Truck?.TruckNumber ?? "غير محدد"
                    })
                    .Select(group => new SummaryItem
                    {
                        Date = group.Key.Date,
                        FarmName = group.Key.FarmName,
                        FactoryName = group.Key.FactoryName,
                        TruckNumber = group.Key.TruckNumber,
                        TotalFarmWeight = group.Sum(s => s.FarmWeight ?? 0),
                        TotalFactoryWeight = group.Sum(s => s.FactoryWeight ?? 0),
                        TotalFreightCost = group.Sum(s => s.FreightCost ?? 0),
                        TotalFarmCost = CalculateTotalFarmCost(group),
                        TotalFactoryRevenue = CalculateTotalFactoryRevenue(group)
                    });

                foreach (var summary in groupedSupplies.OrderByDescending(s => s.Date))
                {
                    AllSummaries.Add(summary);
                }

                ApplyFilters();
                UpdateSummaryCards();
                StatusText.Text = "تم تحميل البيانات بنجاح";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"خطأ في تحميل البيانات: {ex.Message}";
                MessageBox.Show($"حدث خطأ أثناء تحميل البيانات:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal CalculateTotalFarmCost(IGrouping<dynamic, Models.SupplyEntry> group)
        {
            decimal totalCost = 0;

            foreach (var supply in group)
            {
                if (supply.FarmWeight.HasValue && supply.FarmPricePerKilo.HasValue)
                {
                    decimal farmRevenue = supply.FarmWeight.Value * supply.FarmPricePerKilo.Value;
                    decimal farmDiscount = supply.FarmDiscountRate.HasValue ?
                        farmRevenue * (supply.FarmDiscountRate.Value / 100) : 0;
                    totalCost += farmRevenue - farmDiscount + (supply.FreightCost ?? 0);
                }
            }

            return totalCost;
        }

        private decimal CalculateTotalFactoryRevenue(IGrouping<dynamic, Models.SupplyEntry> group)
        {
            decimal totalRevenue = 0;

            foreach (var supply in group)
            {
                if (supply.FactoryWeight.HasValue && supply.FactoryPricePerKilo.HasValue)
                {
                    decimal factoryRevenue = supply.FactoryWeight.Value * supply.FactoryPricePerKilo.Value;
                    decimal factoryDiscount = supply.FactoryDiscountRate.HasValue ?
                        factoryRevenue * (supply.FactoryDiscountRate.Value / 100) : 0;
                    totalRevenue += factoryRevenue - factoryDiscount;
                }
            }

            return totalRevenue;
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = AllSummaries.AsEnumerable();

                // Date filters
                if (FromDatePicker.SelectedDate.HasValue)
                {
                    filtered = filtered.Where(s => s.Date >= FromDatePicker.SelectedDate.Value);
                }

                if (ToDatePicker.SelectedDate.HasValue)
                {
                    filtered = filtered.Where(s => s.Date <= ToDatePicker.SelectedDate.Value);
                }

                // Farm filter
                if (FarmFilter.SelectedItem != null && FarmFilter.SelectedItem.ToString() != "الكل")
                {
                    string selectedFarm = FarmFilter.SelectedItem.ToString();
                    filtered = filtered.Where(s => s.FarmName.Contains(selectedFarm, StringComparison.OrdinalIgnoreCase));
                }

                // Factory filter
                if (FactoryFilter.SelectedItem != null && FactoryFilter.SelectedItem.ToString() != "الكل")
                {
                    string selectedFactory = FactoryFilter.SelectedItem.ToString();
                    filtered = filtered.Where(s => s.FactoryName.Contains(selectedFactory, StringComparison.OrdinalIgnoreCase));
                }

                // Truck number filter
                if (!string.IsNullOrWhiteSpace(TruckNumberFilter.Text))
                {
                    string truckFilter = TruckNumberFilter.Text.Trim();
                    filtered = filtered.Where(s => s.TruckNumber.Contains(truckFilter, StringComparison.OrdinalIgnoreCase));
                }

                FilteredSummaries.Clear();
                foreach (var summary in filtered.OrderByDescending(s => s.Date))
                {
                    FilteredSummaries.Add(summary);
                }

                UpdateSummaryCards();
                StatusText.Text = $"تم تطبيق الفلاتر - {FilteredSummaries.Count} سجل";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"خطأ في تطبيق الفلاتر: {ex.Message}";
                MessageBox.Show($"حدث خطأ أثناء تطبيق الفلاتر:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummaryCards()
        {
            try
            {
                if (FilteredSummaries?.Any() == true)
                {
                    decimal totalFarmWeight = FilteredSummaries.Sum(s => s.TotalFarmWeight);
                    decimal totalFactoryWeight = FilteredSummaries.Sum(s => s.TotalFactoryWeight);
                    decimal totalProfitLoss = FilteredSummaries.Sum(s => s.ProfitLoss);

                    TotalFarmWeightText.Text = $"{totalFarmWeight:N1} كجم";
                    TotalFactoryWeightText.Text = $"{totalFactoryWeight:N1} كجم";
                    TotalProfitLossText.Text = $"{totalProfitLoss:N2} جنيه";

                    // Update profit/loss color
                    if (totalProfitLoss > 0)
                    {
                        TotalProfitLossText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)); // Green
                    }
                    else if (totalProfitLoss < 0)
                    {
                        TotalProfitLossText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)); // Red
                    }
                    else
                    {
                        TotalProfitLossText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)); // Gray
                    }
                }
                else
                {
                    TotalFarmWeightText.Text = "0 كجم";
                    TotalFactoryWeightText.Text = "0 كجم";
                    TotalProfitLossText.Text = "0.00 جنيه";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"خطأ في تحديث البطاقات: {ex.Message}";
            }
        }

        private void GenerateSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FromDatePicker.SelectedDate = DateTime.Today.AddDays(-30);
                ToDatePicker.SelectedDate = DateTime.Today;
                FarmFilter.SelectedIndex = 0;
                FactoryFilter.SelectedIndex = 0;
                TruckNumberFilter.Text = "";

                ApplyFilters();
                StatusText.Text = "تم مسح الفلاتر";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"خطأ في مسح الفلاتر: {ex.Message}";
                MessageBox.Show($"حدث خطأ أثناء مسح الفلاتر:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

   

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadFilters();
            LoadData();
        }

        private void DetailedSummaryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FilteredSummaries?.Any() != true)
                {
                    MessageBox.Show("لا توجد بيانات لعرض الملخص المفصل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal totalFarmWeight = FilteredSummaries.Sum(s => s.TotalFarmWeight);
                decimal totalFactoryWeight = FilteredSummaries.Sum(s => s.TotalFactoryWeight);
                decimal totalFreightCost = FilteredSummaries.Sum(s => s.TotalFreightCost);
                decimal totalFarmCost = FilteredSummaries.Sum(s => s.TotalFarmCost);
                decimal totalFactoryRevenue = FilteredSummaries.Sum(s => s.TotalFactoryRevenue);
                decimal totalProfitLoss = FilteredSummaries.Sum(s => s.ProfitLoss);

                string detailedMessage = $"الملخص المفصل للفترة المحددة:\n\n" +
                    $"إجمالي وزن المزرعة: {totalFarmWeight:N1} كجم\n" +
                    $"إجمالي وزن المصنع: {totalFactoryWeight:N1} كجم\n" +
                    $"إجمالي النولون: {totalFreightCost:N2} جنيه\n" +
                    $"إجمالي أسعار المزرعة: {totalFarmCost:N2} جنيه\n" +
                    $"إجمالي أسعار المصنع: {totalFactoryRevenue:N2} جنيه\n" +
                    $"الربح/الخسارة النهائية: {totalProfitLoss:N2} جنيه\n\n" +
                    $"عدد السجلات: {FilteredSummaries.Count}\n" +
                    $"الفترة الزمنية: من {(FromDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "غير محدد")} إلى {(ToDatePicker.SelectedDate?.ToString("dd/MM/yyyy") ?? "غير محدد")}";

                MessageBox.Show(detailedMessage, "الملخص المفصل", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"خطأ في عرض الملخص المفصل: {ex.Message}";
                MessageBox.Show($"حدث خطأ أثناء عرض الملخص المفصل:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
                else
                {
                    NavigationService.Navigate(new HomePage());
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"خطأ في العودة: {ex.Message}";
                MessageBox.Show($"حدث خطأ أثناء العودة:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SummaryItem : INotifyPropertyChanged
    {
        private DateTime _date;
        private string _farmName;
        private string _factoryName;
        private string _truckNumber;
        private decimal _totalFarmWeight;
        private decimal _totalFactoryWeight;
        private decimal _totalFreightCost;
        private decimal _totalFarmCost;
        private decimal _totalFactoryRevenue;

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged(nameof(Date));
            }
        }

        public string FarmName
        {
            get => _farmName;
            set
            {
                _farmName = value;
                OnPropertyChanged(nameof(FarmName));
            }
        }

        public string FactoryName
        {
            get => _factoryName;
            set
            {
                _factoryName = value;
                OnPropertyChanged(nameof(FactoryName));
            }
        }

        public string TruckNumber
        {
            get => _truckNumber;
            set
            {
                _truckNumber = value;
                OnPropertyChanged(nameof(TruckNumber));
            }
        }

        public decimal TotalFarmWeight
        {
            get => _totalFarmWeight;
            set
            {
                _totalFarmWeight = value;
                OnPropertyChanged(nameof(TotalFarmWeight));
            }
        }

        public decimal TotalFactoryWeight
        {
            get => _totalFactoryWeight;
            set
            {
                _totalFactoryWeight = value;
                OnPropertyChanged(nameof(TotalFactoryWeight));
            }
        }

        public decimal TotalFreightCost
        {
            get => _totalFreightCost;
            set
            {
                _totalFreightCost = value;
                OnPropertyChanged(nameof(TotalFreightCost));
            }
        }

        public decimal TotalFarmCost
        {
            get => _totalFarmCost;
            set
            {
                _totalFarmCost = value;
                OnPropertyChanged(nameof(TotalFarmCost));
            }
        }

        public decimal TotalFactoryRevenue
        {
            get => _totalFactoryRevenue;
            set
            {
                _totalFactoryRevenue = value;
                OnPropertyChanged(nameof(TotalFactoryRevenue));
            }
        }

        public decimal ProfitLoss => TotalFactoryRevenue - TotalFarmCost;

        public decimal ProfitMargin => TotalFactoryRevenue > 0 ? (ProfitLoss / TotalFactoryRevenue) * 100 : 0;

        public int ProfitLossStatus => ProfitLoss > 0 ? 1 : ProfitLoss < 0 ? -1 : 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 