using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Fruittrack.Models;
using System.IO;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;

namespace Fruittrack
{
    public partial class ProductionReportsPage : Page, INotifyPropertyChanged
    {
        private readonly FruitTrackDbContext _context;
        private ObservableCollection<ProductionReportItem> _allReports;
        private ObservableCollection<ProductionReportItem> _filteredReports;

        public ProductionReportsPage()
        {
            InitializeComponent();
            
            // Use dependency injection like other pages
            try
            {
                _context = App.ServiceProvider.GetRequiredService<FruitTrackDbContext>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الاتصال بقاعدة البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            AllReports = new ObservableCollection<ProductionReportItem>();
            FilteredReports = new ObservableCollection<ProductionReportItem>();
            
            DataContext = this;
            ReportsDataGrid.ItemsSource = FilteredReports;
            
            // Initialize filter comboboxes
            ProfitTypeFilter.SelectedIndex = 0;
            
            LoadData();
        }

        public ObservableCollection<ProductionReportItem> AllReports
        {
            get => _allReports;
            set
            {
                _allReports = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ProductionReportItem> FilteredReports
        {
            get => _filteredReports;
            set
            {
                _filteredReports = value;
                OnPropertyChanged();
            }
        }

        private async void LoadData()
        {
            try
            {
                if (_context == null)
                {
                    MessageBox.Show("قاعدة البيانات غير متاحة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Show loading indicator
                LastUpdateText.Text = "جاري تحميل البيانات...";

                // Load all supply entries with related data
                var supplies = await _context.SupplyEntries
                    .Include(s => s.Truck)
                    .Include(s => s.Farm)
                    .Include(s => s.Factory)
                    .ToListAsync();

                // Load farms and factories for filters
                var farms = await _context.Farms.ToListAsync();
                var factories = await _context.Factories.ToListAsync();

                // Populate filter dropdowns
                FarmFilter.Items.Clear();
                FarmFilter.Items.Add(new ComboBoxItem { Content = "جميع المزارع" });
                foreach (var farm in farms)
                {
                    FarmFilter.Items.Add(new ComboBoxItem { Content = farm.FarmName, Tag = farm.FarmId });
                }
                FarmFilter.SelectedIndex = 0;

                FactoryFilter.Items.Clear();
                FactoryFilter.Items.Add(new ComboBoxItem { Content = "جميع المصانع" });
                foreach (var factory in factories)
                {
                    FactoryFilter.Items.Add(new ComboBoxItem { Content = factory.FactoryName, Tag = factory.FactoryId });
                }
                FactoryFilter.SelectedIndex = 0;

                var reportItems = new List<ProductionReportItem>();

                foreach (var supply in supplies)
                {
                    // Calculate profit/loss
                    decimal totalFarmCost = 0;
                    decimal totalFactoryRevenue = 0;
                    decimal profitLoss = 0;
                    decimal profitMargin = 0;
                    int profitLossStatus = 0; // -1: loss, 0: break-even, 1: profit

                    // Calculate farm cost (including transport and discounts)
                    // Prices are now stored per kilo in database
                    if (supply.FarmWeight.HasValue && supply.FarmPricePerKilo.HasValue)
                    {
                        decimal farmRevenue = supply.FarmWeight.Value * supply.FarmPricePerKilo.Value;
                        decimal farmDiscount = supply.FarmDiscountRate.HasValue ? 
                            farmRevenue * (supply.FarmDiscountRate.Value / 100) : 0;
                        totalFarmCost = farmRevenue - farmDiscount + (supply.FreightCost ?? 0);
                    }

                    // Calculate factory revenue (including discounts)
                    // Prices are now stored per kilo in database
                    if (supply.FactoryWeight.HasValue && supply.FactoryPricePerKilo.HasValue)
                    {
                        decimal factoryRevenue = supply.FactoryWeight.Value * supply.FactoryPricePerKilo.Value;
                        decimal factoryDiscount = supply.FactoryDiscountRate.HasValue ? 
                            factoryRevenue * (supply.FactoryDiscountRate.Value / 100) : 0;
                        totalFactoryRevenue = factoryRevenue - factoryDiscount;
                    }

                    // Calculate profit/loss
                    profitLoss = totalFactoryRevenue - totalFarmCost;
                    
                    // Calculate profit margin percentage
                    if (totalFarmCost > 0)
                    {
                        profitMargin = (profitLoss / totalFarmCost) * 100;
                    }

                    // Determine status
                    if (profitLoss > 0) profitLossStatus = 1;
                    else if (profitLoss < 0) profitLossStatus = -1;
                    else profitLossStatus = 0;

                    reportItems.Add(new ProductionReportItem
                    {
                        SupplyEntryId = supply.SupplyEntryId,
                        Date = supply.EntryDate,
                        FarmName = supply.Farm?.FarmName ?? "غير محدد",
                        FactoryName = supply.Factory?.FactoryName ?? "غير محدد",
                        TruckNumber = supply.Truck?.TruckNumber ?? "غير محدد",
                        FarmWeight = supply.FarmWeight ?? 0,
                        FactoryWeight = supply.FactoryWeight ?? 0,
                        FarmPricePerKilo = supply.FarmPricePerKilo ?? 0,
                        FactoryPricePerKilo = supply.FactoryPricePerKilo ?? 0,
                        ProfitLoss = profitLoss,
                        ProfitMargin = profitMargin,
                        ProfitLossStatus = profitLossStatus,
                        TotalFarmCost = totalFarmCost,
                        TotalFactoryRevenue = totalFactoryRevenue
                    });
                }

                // Clear and populate collections
                AllReports.Clear();
                FilteredReports.Clear();

                foreach (var item in reportItems.OrderByDescending(x => x.Date))
                {
                    AllReports.Add(item);
                    FilteredReports.Add(item); // Show all data initially
                }

                // Update summary statistics
                UpdateSummaryStatistics();
                UpdateStatusBar();

                // Show completion message
                var totalSupplies = supplies.Count;
                LastUpdateText.Text = $"آخر تحديث: {DateTime.Now:dd/MM/yyyy HH:mm} | تم تحليل {totalSupplies} توريد";

                MessageBox.Show($"تم تحليل {totalSupplies} توريد بنجاح", 
                              "تحديث مكتمل", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}\n\nتفاصيل الخطأ: {ex.InnerException?.Message}", 
                              "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                
                LastUpdateText.Text = $"خطأ في التحميل: {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
        }

        private void UpdateSummaryStatistics()
        {
            try
            {
                var data = FilteredReports.ToList();

                // Total Gross Weight
                decimal totalGrossWeight = data.Sum(x => x.FarmWeight);
                TotalGrossWeightText.Text = $"{totalGrossWeight:N1} كجم";

                // Total Net Weight
                decimal totalNetWeight = data.Sum(x => x.FactoryWeight);
                TotalNetWeightText.Text = $"{totalNetWeight:N1} كجم";

                // Total Profit/Loss
                decimal totalProfitLoss = data.Sum(x => x.ProfitLoss);
                TotalProfitLossText.Text = $"{totalProfitLoss:N2} جنيه";
                TotalProfitLossText.Foreground = totalProfitLoss >= 0 ? 
                    System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;

                // Best Supplier (highest profit)
                var bestSupplier = data
                    .GroupBy(x => x.FarmName)
                    .Select(g => new 
                    { 
                        FarmName = g.Key, 
                        TotalProfit = g.Sum(x => x.ProfitLoss),
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.TotalProfit)
                    .FirstOrDefault();

                if (bestSupplier != null && bestSupplier.TotalProfit > 0)
                {
                    BestSupplierText.Text = bestSupplier.FarmName;
                    BestSupplierProfitText.Text = $"{bestSupplier.TotalProfit:N2} جنيه";
                    BestSupplierProfitText.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    BestSupplierText.Text = "لا يوجد";
                    BestSupplierProfitText.Text = "0 جنيه";
                    BestSupplierProfitText.Foreground = System.Windows.Media.Brushes.Gray;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث الإحصائيات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatusBar()
        {
            try
            {
                var total = AllReports.Count;
                var filtered = FilteredReports.Count;
                var profitable = FilteredReports.Count(x => x.ProfitLossStatus == 1);
                var losses = FilteredReports.Count(x => x.ProfitLossStatus == -1);

                RecordCountText.Text = $"إجمالي السجلات: {total}";
                FilteredCountText.Text = $"المعروض: {filtered}";
                ProfitableCountText.Text = $"مربح: {profitable}";
                LossCountText.Text = $"خسارة: {losses}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث شريط الحالة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = AllReports.AsEnumerable();

                // Date filter
                if (FromDatePicker.SelectedDate.HasValue)
                    filtered = filtered.Where(x => x.Date >= FromDatePicker.SelectedDate.Value);

                if (ToDatePicker.SelectedDate.HasValue)
                    filtered = filtered.Where(x => x.Date <= ToDatePicker.SelectedDate.Value);

                // Farm filter
                if (FarmFilter.SelectedItem is ComboBoxItem farmItem && 
                    farmItem.Content?.ToString() != "جميع المزارع" && 
                    !string.IsNullOrEmpty(farmItem.Content?.ToString()))
                {
                    var farmName = farmItem.Content.ToString();
                    filtered = filtered.Where(x => x.FarmName.Contains(farmName, StringComparison.OrdinalIgnoreCase));
                }

                // Factory filter
                if (FactoryFilter.SelectedItem is ComboBoxItem factoryItem && 
                    factoryItem.Content?.ToString() != "جميع المصانع" && 
                    !string.IsNullOrEmpty(factoryItem.Content?.ToString()))
                {
                    var factoryName = factoryItem.Content.ToString();
                    filtered = filtered.Where(x => x.FactoryName.Contains(factoryName, StringComparison.OrdinalIgnoreCase));
                }

                // Truck number filter
                if (!string.IsNullOrWhiteSpace(TruckNumberFilter.Text))
                {
                    filtered = filtered.Where(x => x.TruckNumber.Contains(TruckNumberFilter.Text, StringComparison.OrdinalIgnoreCase));
                }

                // Profit type filter
                if (ProfitTypeFilter.SelectedItem is ComboBoxItem profitItem && 
                    !string.IsNullOrEmpty(profitItem.Content?.ToString()))
                {
                    switch (profitItem.Content.ToString())
                    {
                        case "ربح فقط":
                            filtered = filtered.Where(x => x.ProfitLossStatus == 1);
                            break;
                        case "خسارة فقط":
                            filtered = filtered.Where(x => x.ProfitLossStatus == -1);
                            break;
                        case "متعادل":
                            filtered = filtered.Where(x => x.ProfitLossStatus == 0);
                            break;
                    }
                }

                FilteredReports.Clear();
                foreach (var item in filtered)
                {
                    FilteredReports.Add(item);
                }

                UpdateSummaryStatistics();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تطبيق الفلاتر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            FarmFilter.SelectedIndex = 0;
            FactoryFilter.SelectedIndex = 0;
            TruckNumberFilter.Text = "";
            ProfitTypeFilter.SelectedIndex = 0;

            // Reset to show all data
            FilteredReports.Clear();
            foreach (var item in AllReports)
            {
                FilteredReports.Add(item);
            }

            UpdateSummaryStatistics();
            UpdateStatusBar();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void ExportReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV Files|*.csv",
                    DefaultExt = "csv",
                    FileName = $"تقرير_الإنتاج_والتوزيع_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCSV(saveDialog.FileName);
                    MessageBox.Show("تم تصدير التقرير بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تصدير التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCSV(string fileName)
        {
            var lines = new List<string>
            {
                "التاريخ,اسم المزرعة,اسم المصنع,رقم العربية,الوزن الإجمالي (كجم),الوزن الصافي (كجم),سعر المزرعة (جنيه/كجم),سعر المصنع (جنيه/كجم),الربح/الخسارة (جنيه),هامش الربح %"
            };

            // Add summary statistics
            lines.Add($"إحصائيات التقرير:,,,,,,,,");
            lines.Add($"إجمالي الوزن الإجمالي:,{FilteredReports.Sum(x => x.FarmWeight):N1} كجم,,,,,,,");
            lines.Add($"إجمالي الوزن الصافي:,{FilteredReports.Sum(x => x.FactoryWeight):N1} كجم,,,,,,,");
            lines.Add($"إجمالي الربح/الخسارة:,{FilteredReports.Sum(x => x.ProfitLoss):N2} جنيه,,,,,,,");
            lines.Add(",,,,,,,,");
            lines.Add("تفاصيل التوريدات:,,,,,,,,");

            foreach (var item in FilteredReports)
            {
                var line = $"{item.Date:dd/MM/yyyy},{item.FarmName},{item.FactoryName},{item.TruckNumber}," +
                          $"{item.FarmWeight:N1},{item.FactoryWeight:N1},{item.FarmPricePerKilo:N3},{item.FactoryPricePerKilo:N3}," +
                          $"{item.ProfitLoss:N2},{item.ProfitMargin:N1}";
                lines.Add(line);
            }

            File.WriteAllLines(fileName, lines, System.Text.Encoding.UTF8);
        }

        private void DetailedReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create detailed analysis
                var totalRevenue = FilteredReports.Sum(x => x.TotalFactoryRevenue);
                var totalCost = FilteredReports.Sum(x => x.TotalFarmCost);
                var overallProfitMargin = totalCost > 0 ? ((totalRevenue - totalCost) / totalCost * 100) : 0;

                var profitable = FilteredReports.Where(x => x.ProfitLossStatus == 1);
                var losses = FilteredReports.Where(x => x.ProfitLossStatus == -1);

                var message = $"تقرير مفصل للإنتاج والتوزيع\n" +
                             $"==============================\n\n" +
                             $"إجمالي الإيرادات: {totalRevenue:N2} جنيه\n" +
                             $"إجمالي التكاليف: {totalCost:N2} جنيه\n" +
                             $"صافي الربح/الخسارة: {(totalRevenue - totalCost):N2} جنيه\n" +
                             $"هامش الربح الإجمالي: {overallProfitMargin:N1}%\n\n" +
                             $"التوريدات المربحة: {profitable.Count()}\n" +
                             $"متوسط ربح التوريد: {(profitable.Any() ? profitable.Average(x => x.ProfitLoss) : 0):N2} جنيه\n\n" +
                             $"التوريدات الخاسرة: {losses.Count()}\n" +
                             $"متوسط خسارة التوريد: {(losses.Any() ? Math.Abs(losses.Average(x => x.ProfitLoss)) : 0):N2} جنيه\n\n" +
                             $"إجمالي الوزن المعالج: {FilteredReports.Sum(x => x.FarmWeight):N1} كجم\n" +
                             $"إجمالي الوزن المسلم: {FilteredReports.Sum(x => x.FactoryWeight):N1} كجم\n" +
                             $"نسبة الفاقد: {(FilteredReports.Sum(x => x.FarmWeight) > 0 ? ((FilteredReports.Sum(x => x.FarmWeight) - FilteredReports.Sum(x => x.FactoryWeight)) / FilteredReports.Sum(x => x.FarmWeight) * 100) : 0):N1}%";

                MessageBox.Show(message, "تقرير مفصل", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء التقرير المفصل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null)
                NavigationService.GoBack();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Model class for production report items
    public class ProductionReportItem : INotifyPropertyChanged
    {
        private int _supplyEntryId;
        private DateTime _date;
        private string _farmName;
        private string _factoryName;
        private string _truckNumber;
        private decimal _farmWeight;
        private decimal _factoryWeight;
        private decimal _farmPricePerKilo;
        private decimal _factoryPricePerKilo;
        private decimal _profitLoss;
        private decimal _profitMargin;
        private int _profitLossStatus;
        private decimal _totalFarmCost;
        private decimal _totalFactoryRevenue;

        public int SupplyEntryId
        {
            get => _supplyEntryId;
            set { _supplyEntryId = value; OnPropertyChanged(); }
        }

        public DateTime Date
        {
            get => _date;
            set { _date = value; OnPropertyChanged(); }
        }

        public string FarmName
        {
            get => _farmName;
            set { _farmName = value; OnPropertyChanged(); }
        }

        public string FactoryName
        {
            get => _factoryName;
            set { _factoryName = value; OnPropertyChanged(); }
        }

        public string TruckNumber
        {
            get => _truckNumber;
            set { _truckNumber = value; OnPropertyChanged(); }
        }

        public decimal FarmWeight
        {
            get => _farmWeight;
            set { _farmWeight = value; OnPropertyChanged(); }
        }

        public decimal FactoryWeight
        {
            get => _factoryWeight;
            set { _factoryWeight = value; OnPropertyChanged(); }
        }

        public decimal FarmPricePerKilo
        {
            get => _farmPricePerKilo;
            set { _farmPricePerKilo = value; OnPropertyChanged(); }
        }

        public decimal FactoryPricePerKilo
        {
            get => _factoryPricePerKilo;
            set { _factoryPricePerKilo = value; OnPropertyChanged(); }
        }

        public decimal ProfitLoss
        {
            get => _profitLoss;
            set { _profitLoss = value; OnPropertyChanged(); }
        }

        public decimal ProfitMargin
        {
            get => _profitMargin;
            set { _profitMargin = value; OnPropertyChanged(); }
        }

        public int ProfitLossStatus
        {
            get => _profitLossStatus;
            set { _profitLossStatus = value; OnPropertyChanged(); }
        }

        public decimal TotalFarmCost
        {
            get => _totalFarmCost;
            set { _totalFarmCost = value; OnPropertyChanged(); }
        }

        public decimal TotalFactoryRevenue
        {
            get => _totalFactoryRevenue;
            set { _totalFactoryRevenue = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 