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
using Fruittrack.Utilities;
using System.Text;

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
            
            // Apply filters automatically when inputs change
            FromDatePicker.SelectedDateChanged += (_, __) => ApplyFilters();
            ToDatePicker.SelectedDateChanged += (_, __) => ApplyFilters();
            FarmFilter.SelectionChanged += (_, __) => ApplyFilters();
            FactoryFilter.SelectionChanged += (_, __) => ApplyFilters();
            ProfitTypeFilter.SelectionChanged += (_, __) => ApplyFilters();
            TruckNumberFilter.TextChanged += (_, __) => ApplyFilters();
            
            LoadData();
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            ExportUtilities.ExportToTemporaryPdfAndOpen(this, "تقارير الإنتاج والتوزيع");
        }

        private void PdfButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("تقارير_الإنتاج_والتوزيع.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportToPdf(this, filePath, "تقارير الإنتاج والتوزيع");
            }
        }

        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("تقارير_الإنتاج_والتوزيع.xlsx", "Excel files (*.xlsx)|*.xlsx");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportDataGridToExcel(ReportsDataGrid, filePath, "تقارير الإنتاج والتوزيع");
            }
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

                FactoryFilter.Items.Clear();
                FactoryFilter.Items.Add(new ComboBoxItem { Content = "جميع المصانع" });
                foreach (var factory in factories)
                {
                    FactoryFilter.Items.Add(new ComboBoxItem { Content = factory.FactoryName, Tag = factory.FactoryId });
                }

                // Clear existing data
                AllReports.Clear();

                // Process each supply entry
                foreach (var supply in supplies)
                {
                    var reportItem = new ProductionReportItem
                    {
                        SupplyEntryId = supply.SupplyEntryId,
                        Date = supply.EntryDate,
                        FarmName = supply.Farm?.FarmName ?? "غير محدد",
                        FactoryName = supply.Factory?.FactoryName ?? "غير محدد",
                        TruckNumber = supply.Truck?.TruckNumber ?? "غير محدد",
                        FarmWeight = supply.FarmWeight ?? 0,
                        FactoryWeight = supply.FactoryWeight ?? 0,
                        FarmPricePerKilo = supply.FarmPricePerKilo ?? 0,
                        FactoryPricePerKilo = supply.FactoryPricePerKilo ?? 0
                    };

                    // Calculate totals and profit/loss based on SupplyRecordPage logic (apply discount to allowed weights)
                    var farmDiscount = supply.FarmDiscountRate ?? 0m;
                    var factoryDiscount = supply.FactoryDiscountRate ?? 0m;
                    var farmAllowed = reportItem.FarmWeight * (1 - (farmDiscount / 100m));
                    var factoryAllowed = reportItem.FactoryWeight * (1 - (factoryDiscount / 100m));
                    reportItem.TotalFarmCost = farmAllowed * reportItem.FarmPricePerKilo;
                    reportItem.TotalFactoryRevenue = factoryAllowed * reportItem.FactoryPricePerKilo;

                    // Fetch Freight (nolon) and Contractor from Contractor table
                    reportItem.FreightPrice = supply.FreightCost ?? 0m;
                    
                    // Get contractor name from Contractor table based on farm name relationship
                    string contractorName = string.Empty;
                    if (!string.IsNullOrEmpty(supply.Farm?.FarmName))
                    {
                        var contractor = _context.Contractors
                            .Where(c => c.RelatedFramName == supply.Farm.FarmName)
                            .Select(c => c.ContractorName)
                            .FirstOrDefault();
                        contractorName = contractor ?? string.Empty;
                    }
                    reportItem.ContractorName = contractorName;
                    reportItem.ProfitLoss = reportItem.TotalFactoryRevenue - (reportItem.TotalFarmCost + reportItem.FreightPrice);

                    // Calculate profit margin percentage
                    if (reportItem.TotalFarmCost > 0)
                    {
                        reportItem.ProfitMargin = (reportItem.ProfitLoss / reportItem.TotalFarmCost) * 100;
                    }

                    // Set profit/loss status (1 = profit, 0 = break-even, -1 = loss)
                    if (reportItem.ProfitLoss > 0)
                        reportItem.ProfitLossStatus = 1;
                    else if (reportItem.ProfitLoss < 0)
                        reportItem.ProfitLossStatus = -1;
                    else
                        reportItem.ProfitLossStatus = 0;

                    AllReports.Add(reportItem);
                }

                // Apply initial filters
                ApplyFilters();

                // Update summary statistics
                UpdateSummaryStatistics();

                // Update status
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                LastUpdateText.Text = "حدث خطأ في تحميل البيانات";
            }
        }

        private void UpdateSummaryStatistics()
        {
            try
            {
                var totalGrossWeight = FilteredReports.Sum(r => r.FarmWeight);
                var totalNetWeight = FilteredReports.Sum(r => r.FactoryWeight);
                var totalProfitLoss = FilteredReports.Sum(r => r.ProfitLoss);

                //TotalGrossWeightText.Text = $"{totalGrossWeight:N1} كجم";
                //TotalNetWeightText.Text = $"{totalNetWeight:N1} كجم";
                TotalProfitLossText.Text = $"{totalProfitLoss:N0} جنيه";

                // مؤشر: نسبة الربح إلى إجمالي إيرادات المصنع لجميع السجلات المعروضة
                decimal totalFactoryRevenue = FilteredReports.Sum(r => r.TotalFactoryRevenue);
                decimal indicatorPercent = totalFactoryRevenue > 0 ? (totalProfitLoss / totalFactoryRevenue) * 100m : 0m;
                if (IndicatorText != null)
                {
                    IndicatorText.Text = $"{indicatorPercent:N1}%";
                    if (indicatorPercent > 0)
                    {
                        IndicatorText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129));
                    }
                    else if (indicatorPercent < 0)
                    {
                        IndicatorText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
                    }
                    else
                    {
                        IndicatorText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128));
                    }
                }

                // Find best supplier (highest profit)
                var bestSupplier = FilteredReports
                    .Where(r => r.ProfitLoss > 0)
                    .OrderByDescending(r => r.ProfitLoss)
                    .FirstOrDefault();

                if (bestSupplier != null)
                {
                    BestSupplierText.Text = bestSupplier.FarmName;
                    BestSupplierProfitText.Text = $"{bestSupplier.ProfitLoss:N0} جنيه";
                }
                else
                {
                    BestSupplierText.Text = "غير محدد";
                    BestSupplierProfitText.Text = "0 جنيه";
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
                var totalRecords = FilteredReports.Count;
                var profitableRecords = FilteredReports.Count(r => r.ProfitLoss > 0);
                var lossRecords = FilteredReports.Count(r => r.ProfitLoss < 0);
                var breakEvenRecords = FilteredReports.Count(r => r.ProfitLoss == 0);

                RecordCountText.Text = $"إجمالي السجلات: {totalRecords}";
                //ProfitableRecordsText.Text = $"سجلات ربحية: {profitableRecords}";
                //LossRecordsText.Text = $"سجلات خسارة: {lossRecords}";
                //BreakEvenRecordsText.Text = $"سجلات تعادل: {breakEvenRecords}";

                LastUpdateText.Text = $"آخر تحديث: {DateTime.Now:dd/MM/yyyy HH:mm}";
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

                // Date range filter
                if (FromDatePicker.SelectedDate.HasValue)
                {
                    filtered = filtered.Where(r => r.Date >= FromDatePicker.SelectedDate.Value);
                }

                if (ToDatePicker.SelectedDate.HasValue)
                {
                    filtered = filtered.Where(r => r.Date <= ToDatePicker.SelectedDate.Value);
                }

                // Farm filter
                if (FarmFilter.SelectedItem is ComboBoxItem selectedFarmItem && selectedFarmItem.Tag != null)
                {
                    int farmId = (int)selectedFarmItem.Tag;
                    filtered = filtered.Where(r => r.FarmName == selectedFarmItem.Content.ToString());
                }

                // Factory filter
                if (FactoryFilter.SelectedItem is ComboBoxItem selectedFactoryItem && selectedFactoryItem.Tag != null)
                {
                    int factoryId = (int)selectedFactoryItem.Tag;
                    filtered = filtered.Where(r => r.FactoryName == selectedFactoryItem.Content.ToString());
                }

                // Truck number filter
                if (!string.IsNullOrWhiteSpace(TruckNumberFilter.Text))
                {
                    string truckFilter = TruckNumberFilter.Text.Trim();
                    filtered = filtered.Where(r => !string.IsNullOrEmpty(r.TruckNumber) && r.TruckNumber.Contains(truckFilter, StringComparison.OrdinalIgnoreCase));
                }

                // Profit type filter (values: الجميع, ربح فقط, خسارة فقط, متعادل)
                if (ProfitTypeFilter.SelectedItem is ComboBoxItem selectedProfitItem)
                {
                    string profitType = selectedProfitItem.Content?.ToString() ?? string.Empty;
                    if (profitType == "ربح فقط")
                    {
                        filtered = filtered.Where(r => r.ProfitLoss > 0);
                    }
                    else if (profitType == "خسارة فقط")
                    {
                        filtered = filtered.Where(r => r.ProfitLoss < 0);
                    }
                    else if (profitType == "متعادل")
                    {
                        filtered = filtered.Where(r => r.ProfitLoss == 0);
                    }
                    // "الجميع" leaves filtered as-is
                }

                // Update filtered collection
                FilteredReports.Clear();
                foreach (var item in filtered)
                {
                    FilteredReports.Add(item);
                }

                // Update statistics
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
            try
            {
                // Clear all filters
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            FarmFilter.SelectedIndex = 0;
            FactoryFilter.SelectedIndex = 0;
            ProfitTypeFilter.SelectedIndex = 0;

                // Apply filters to show all data
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في مسح الفلاتر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void ExportToCSV(string fileName)
        {
            try
            {
                using var writer = new StreamWriter(fileName, false, Encoding.UTF8);
                
                // Write header
                writer.WriteLine("التاريخ,المزرعة,المصنع,رقم العربية,وزن المزرعة,وزن المصنع,سعر المزرعة,سعر المصنع,إجمالي تكلفة المزرعة,إجمالي إيرادات المصنع,الربح/الخسارة,نسبة الربح");

                // Write data
            foreach (var item in FilteredReports)
                {
                    writer.WriteLine($"{item.Date:dd/MM/yyyy}," +
                                   $"{item.FarmName}," +
                                   $"{item.FactoryName}," +
                                   $"{item.TruckNumber}," +
                                   $"{item.FarmWeight}," +
                                   $"{item.FactoryWeight}," +
                                   $"{item.FarmPricePerKilo}," +
                                   $"{item.FactoryPricePerKilo}," +
                                   $"{item.TotalFarmCost}," +
                                   $"{item.TotalFactoryRevenue}," +
                                   $"{item.ProfitLoss}," +
                                   $"{item.ProfitMargin:F2}%");
                }

                MessageBox.Show($"تم تصدير التقرير بنجاح إلى: {fileName}", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الملف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DetailedReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var detailedReport = new StringBuilder();
                detailedReport.AppendLine("تقرير مفصل للإنتاج والتوزيع");
                detailedReport.AppendLine(new string('=', 50));
                detailedReport.AppendLine($"التاريخ: {DateTime.Now:dd/MM/yyyy HH:mm}");
                detailedReport.AppendLine();

                // Summary statistics
                var totalGrossWeight = FilteredReports.Sum(r => r.FarmWeight);
                var totalNetWeight = FilteredReports.Sum(r => r.FactoryWeight);
                var totalProfitLoss = FilteredReports.Sum(r => r.ProfitLoss);
                var profitableRecords = FilteredReports.Count(r => r.ProfitLoss > 0);
                var lossRecords = FilteredReports.Count(r => r.ProfitLoss < 0);

                detailedReport.AppendLine("الإحصائيات الإجمالية:");
                detailedReport.AppendLine($"إجمالي الوزن الإجمالي: {totalGrossWeight:N1} كجم");
                detailedReport.AppendLine($"إجمالي الوزن الصافي: {totalNetWeight:N1} كجم");
                detailedReport.AppendLine($"إجمالي الربح/الخسارة: {totalProfitLoss:N0} جنيه");
                detailedReport.AppendLine($"عدد السجلات الربحية: {profitableRecords}");
                detailedReport.AppendLine($"عدد السجلات الخاسرة: {lossRecords}");
                detailedReport.AppendLine();

                // Detailed records
                detailedReport.AppendLine("التفاصيل:");
                detailedReport.AppendLine(new string('=', 50));

                foreach (var item in FilteredReports)
                {
                    detailedReport.AppendLine($"التاريخ: {item.Date:dd/MM/yyyy}");
                    detailedReport.AppendLine($"المزرعة: {item.FarmName}");
                    detailedReport.AppendLine($"المصنع: {item.FactoryName}");
                    detailedReport.AppendLine($"رقم العربية: {item.TruckNumber}");
                    detailedReport.AppendLine($"وزن المزرعة: {item.FarmWeight:N1} كجم");
                    detailedReport.AppendLine($"وزن المصنع: {item.FactoryWeight:N1} كجم");
                    detailedReport.AppendLine($"الربح/الخسارة: {item.ProfitLoss:N0} جنيه");
                    detailedReport.AppendLine();
                }

                // Show detailed report
                var result = MessageBox.Show(detailedReport.ToString(), "تقرير مفصل", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء التقرير المفصل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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
        private string _contractorName;
        private decimal _freightPrice;

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

        public string ContractorName
        {
            get => _contractorName;
            set { _contractorName = value; OnPropertyChanged(); }
        }

        public decimal FreightPrice
        {
            get => _freightPrice;
            set { _freightPrice = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 