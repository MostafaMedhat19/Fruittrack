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
    public partial class InvalidSuppliesPage : Page, INotifyPropertyChanged
    {
        private readonly FruitTrackDbContext _context;
        private ObservableCollection<InvalidSupplyItem> _invalidSupplies;
        private ObservableCollection<InvalidSupplyItem> _filteredSupplies;

        public InvalidSuppliesPage()
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
            
            InvalidSupplies = new ObservableCollection<InvalidSupplyItem>();
            FilteredSupplies = new ObservableCollection<InvalidSupplyItem>();
            
            DataContext = this;
            InvalidSuppliesDataGrid.ItemsSource = FilteredSupplies;
            
            // Initialize filter comboboxes
            ErrorTypeFilter.SelectedIndex = 0;
            PriorityFilter.SelectedIndex = 0;
            
            // Wire up event handlers for filters
            ErrorTypeFilter.SelectionChanged += (s, e) => ApplyFilters();
            FactoryFilter.SelectionChanged += (s, e) => ApplyFilters();
            TruckNumberFilter.TextChanged += (s, e) => ApplyFilters();
            PriorityFilter.SelectionChanged += (s, e) => ApplyFilters();
            
            LoadData();
        }

        public ObservableCollection<InvalidSupplyItem> InvalidSupplies
        {
            get => _invalidSupplies;
            set
            {
                _invalidSupplies = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<InvalidSupplyItem> FilteredSupplies
        {
            get => _filteredSupplies;
            set
            {
                _filteredSupplies = value;
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

                // Load ALL supply entries with related data
                var supplies = await _context.SupplyEntries
                    .Include(s => s.Truck)
                    .Include(s => s.Farm)
                    .Include(s => s.Factory)
                    .ToListAsync();

                var invalidItems = new List<InvalidSupplyItem>();

                // Check ALL supplies for issues
                foreach (var supply in supplies)
                {
                    bool hasIssue = false;

                    // Check for zero factory weight
                    if (supply.FactoryWeight.HasValue && supply.FactoryWeight.Value == 0)
                    {
                        invalidItems.Add(new InvalidSupplyItem
                        {
                            SupplyEntryId = supply.SupplyEntryId,
                            Date = supply.EntryDate,
                            TruckNumber = supply.Truck?.TruckNumber ?? "غير محدد",
                            FarmName = supply.Farm?.FarmName ?? "غير محدد",
                            FactoryName = supply.Factory?.FactoryName ?? "غير محدد",
                            FactoryWeight = supply.FactoryWeight,
                            ErrorType = "وزن صفر",
                            ErrorDescription = "وزن المصنع مسجل بقيمة صفر",
                            SuggestedFix = "قم بتحديث وزن المصنع بالقيمة الصحيحة",
                            Priority = "عالي"
                        });
                        hasIssue = true;
                    }

                    // Check for null factory weight
                    if (!supply.FactoryWeight.HasValue)
                    {
                        invalidItems.Add(new InvalidSupplyItem
                        {
                            SupplyEntryId = supply.SupplyEntryId,
                            Date = supply.EntryDate,
                            TruckNumber = supply.Truck?.TruckNumber ?? "غير محدد",
                            FarmName = supply.Farm?.FarmName ?? "غير محدد",
                            FactoryName = supply.Factory?.FactoryName ?? "غير محدد",
                            FactoryWeight = supply.FactoryWeight,
                            ErrorType = "وزن غير مسجل",
                            ErrorDescription = "وزن المصنع غير مسجل في النظام",
                            SuggestedFix = "قم بإدخال وزن المصنع",
                            Priority = "متوسط"
                        });
                        hasIssue = true;
                    }

                    // Check for invalid factory
                    if (supply.FactoryId == null || supply.Factory == null)
                    {
                        invalidItems.Add(new InvalidSupplyItem
                        {
                            SupplyEntryId = supply.SupplyEntryId,
                            Date = supply.EntryDate,
                            TruckNumber = supply.Truck?.TruckNumber ?? "غير محدد",
                            FarmName = supply.Farm?.FarmName ?? "غير محدد",
                            FactoryName = "مصنع غير صحيح",
                            FactoryWeight = supply.FactoryWeight,
                            ErrorType = "مصنع غير صحيح",
                            ErrorDescription = "المصنع المرتبط بهذا التوريد غير صحيح أو محذوف",
                            SuggestedFix = "قم بتحديد مصنع صحيح من القائمة",
                            Priority = "عالي"
                        });
                        hasIssue = true;
                    }

                    // Check for negative factory weight
                    if (supply.FactoryWeight.HasValue && supply.FactoryWeight.Value < 0)
                    {
                        invalidItems.Add(new InvalidSupplyItem
                        {
                            SupplyEntryId = supply.SupplyEntryId,
                            Date = supply.EntryDate,
                            TruckNumber = supply.Truck?.TruckNumber ?? "غير محدد",
                            FarmName = supply.Farm?.FarmName ?? "غير محدد",
                            FactoryName = supply.Factory?.FactoryName ?? "غير محدد",
                            FactoryWeight = supply.FactoryWeight,
                            ErrorType = "وزن سالب",
                            ErrorDescription = "وزن المصنع لا يمكن أن يكون سالباً",
                            SuggestedFix = "قم بتصحيح الوزن إلى قيمة موجبة",
                            Priority = "عالي"
                        });
                        hasIssue = true;
                    }

                    // Check for missing truck
                    if (supply.TruckId == null || supply.Truck == null)
                    {
                        invalidItems.Add(new InvalidSupplyItem
                        {
                            SupplyEntryId = supply.SupplyEntryId,
                            Date = supply.EntryDate,
                            TruckNumber = "غير محدد",
                            FarmName = supply.Farm?.FarmName ?? "غير محدد",
                            FactoryName = supply.Factory?.FactoryName ?? "غير محدد",
                            FactoryWeight = supply.FactoryWeight,
                            ErrorType = "عربية غير محددة",
                            ErrorDescription = "العربية المرتبطة بهذا التوريد غير محددة",
                            SuggestedFix = "قم بتحديد العربية المناسبة",
                            Priority = "متوسط"
                        });
                        hasIssue = true;
                    }

                    // Check for missing farm
                    if (supply.FarmId == null || supply.Farm == null)
                    {
                        invalidItems.Add(new InvalidSupplyItem
                        {
                            SupplyEntryId = supply.SupplyEntryId,
                            Date = supply.EntryDate,
                            TruckNumber = supply.Truck?.TruckNumber ?? "غير محدد",
                            FarmName = "غير محدد",
                            FactoryName = supply.Factory?.FactoryName ?? "غير محدد",
                            FactoryWeight = supply.FactoryWeight,
                            ErrorType = "مزرعة غير محددة",
                            ErrorDescription = "المزرعة المرتبطة بهذا التوريد غير محددة",
                            SuggestedFix = "قم بتحديد المزرعة المناسبة",
                            Priority = "متوسط"
                        });
                        hasIssue = true;
                    }
                }

                // Clear and populate the collections
                InvalidSupplies.Clear();
                FilteredSupplies.Clear();

                // Add all invalid items
                foreach (var item in invalidItems.OrderByDescending(x => x.Date))
                {
                    InvalidSupplies.Add(item);
                    FilteredSupplies.Add(item); // Show ALL data initially
                }

                // Update summary cards and status
                UpdateSummaryCards();
                UpdateStatusBar();

                // Load factories for the filter ComboBox
                await LoadFactoryFilter();

                // Show completion message
                var totalSupplies = supplies.Count;
                var invalidCount = invalidItems.Count;
                LastUpdateText.Text = $"آخر تحديث: {DateTime.Now:dd/MM/yyyy HH:mm} | فحص {totalSupplies} توريد";

                // Show message about results
                if (invalidCount == 0)
                {
                    MessageBox.Show($"تم فحص {totalSupplies} توريد\nلم يتم العثور على أي مشاكل!", 
                                  "فحص مكتمل", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"تم فحص {totalSupplies} توريد\nتم العثور على {invalidCount} مشكلة", 
                                  "فحص مكتمل", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}\n\nتفاصيل الخطأ: {ex.InnerException?.Message}", 
                              "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Reset UI
                LastUpdateText.Text = $"خطأ في التحميل: {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
        }

        private async Task LoadFactoryFilter()
        {
            try
            {
                if (_context == null) return;

                // Get all unique factories from the database
                var factories = await _context.Factories
                    .OrderBy(f => f.FactoryName)
                    .ToListAsync();

                // Clear the ComboBox
                FactoryFilter.Items.Clear();

                // Add "All" option first
                var allItem = new ComboBoxItem
                {
                    Content = "جميع المصانع",
                    Tag = "ALL"
                };
                FactoryFilter.Items.Add(allItem);

                // Add each factory
                foreach (var factory in factories)
                {
                    var item = new ComboBoxItem
                    {
                        Content = factory.FactoryName,
                        Tag = factory.FactoryId
                    };
                    FactoryFilter.Items.Add(item);
                }

                // Set default selection to "All"
                FactoryFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل قائمة المصانع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = InvalidSupplies.AsEnumerable();

                // Date filter
                if (FromDatePicker.SelectedDate.HasValue)
                    filtered = filtered.Where(x => x.Date >= FromDatePicker.SelectedDate.Value);

                if (ToDatePicker.SelectedDate.HasValue)
                    filtered = filtered.Where(x => x.Date <= ToDatePicker.SelectedDate.Value);

                // Error type filter
                if (ErrorTypeFilter.SelectedItem is ComboBoxItem errorType && 
                    errorType.Content.ToString() != "جميع الأخطاء")
                {
                    filtered = filtered.Where(x => x.ErrorType.Contains(errorType.Content.ToString()));
                }

                // Factory filter
                if (FactoryFilter.SelectedItem is ComboBoxItem factoryItem && 
                    factoryItem.Tag?.ToString() != "ALL")
                {
                    var selectedFactoryName = factoryItem.Content.ToString();
                    filtered = filtered.Where(x => x.FactoryName == selectedFactoryName);
                }

                // Truck number filter
                if (!string.IsNullOrWhiteSpace(TruckNumberFilter.Text))
                {
                    filtered = filtered.Where(x => x.TruckNumber.Contains(TruckNumberFilter.Text, StringComparison.OrdinalIgnoreCase));
                }

                // Priority filter
                if (PriorityFilter.SelectedItem is ComboBoxItem priority && 
                    priority.Content.ToString() != "جميع الأولويات")
                {
                    filtered = filtered.Where(x => x.Priority == priority.Content.ToString());
                }

                FilteredSupplies.Clear();
                foreach (var item in filtered)
                {
                    FilteredSupplies.Add(item);
                }

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تطبيق الفلاتر: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummaryCards()
        {
            try
            {
                ZeroWeightCount.Text = InvalidSupplies.Count(x => x.ErrorType == "وزن صفر").ToString();
                NullWeightCount.Text = InvalidSupplies.Count(x => x.ErrorType == "وزن غير مسجل").ToString();
                InvalidFactoryCount.Text = InvalidSupplies.Count(x => x.ErrorType == "مصنع غير صحيح").ToString();
                TotalIssuesCount.Text = InvalidSupplies.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث البطاقات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatusBar()
        {
            try
            {
                RecordCountText.Text = $"إجمالي السجلات المشكوك فيها: {InvalidSupplies.Count}";
                FilteredCountText.Text = $"المعروض: {FilteredSupplies.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث شريط الحالة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            FromDatePicker.SelectedDate = null;
            ToDatePicker.SelectedDate = null;
            ErrorTypeFilter.SelectedIndex = 0;
                            FactoryFilter.SelectedIndex = 0; // Reset to "All"
            TruckNumberFilter.Text = "";
            PriorityFilter.SelectedIndex = 0;
            
            ApplyFilters();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private async void FixButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is InvalidSupplyItem item)
            {
                var result = MessageBox.Show(
                    $"هل تريد إصلاح هذه المشكلة؟\n\nالخطأ: {item.ErrorDescription}\nالحل المقترح: {item.SuggestedFix}",
                    "تأكيد الإصلاح", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var supply = await _context.SupplyEntries.FindAsync(item.SupplyEntryId);
                        if (supply != null)
                        {
                            // Handle different error types
                            switch (item.ErrorType)
                            {
                                case "وزن صفر":
                                case "وزن غير مسجل":
                                    // Open a dialog to enter correct weight
                                    var weightDialog = new WeightInputDialog();
                                    if (weightDialog.ShowDialog() == true)
                                    {
                                        supply.FactoryWeight = weightDialog.Weight;
                                        await _context.SaveChangesAsync();
                                        MessageBox.Show("تم إصلاح المشكلة بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                                        LoadData(); // Refresh data
                                    }
                                    break;

                                case "مصنع غير صحيح":
                                    // Open a dialog to select correct factory
                                    var factoryDialog = new FactorySelectionDialog(_context);
                                    if (factoryDialog.ShowDialog() == true)
                                    {
                                        supply.FactoryId = factoryDialog.SelectedFactoryId;
                                        await _context.SaveChangesAsync();
                                        MessageBox.Show("تم إصلاح المشكلة بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                                        LoadData(); // Refresh data
                                    }
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ في إصلاح المشكلة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void IgnoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is InvalidSupplyItem item)
            {
                var result = MessageBox.Show(
                    "هل تريد تجاهل هذه المشكلة؟\nسيتم إخفاؤها من القائمة مؤقتاً.",
                    "تأكيد التجاهل", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    InvalidSupplies.Remove(item);
                    FilteredSupplies.Remove(item);
                    UpdateSummaryCards();
                    UpdateStatusBar();
                }
            }
        }

        private async void FixAllButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "هل تريد محاولة إصلاح جميع المشاكل الممكنة تلقائياً؟\n\nسيتم إصلاح المشاكل البسيطة فقط.",
                "تأكيد الإصلاح الشامل", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    int fixedCount = 0;

                    foreach (var item in FilteredSupplies.ToList())
                    {
                        var supply = await _context.SupplyEntries.FindAsync(item.SupplyEntryId);
                        if (supply != null)
                        {
                            // Only auto-fix simple cases
                            if (item.ErrorType == "وزن صفر" && supply.FarmWeight.HasValue && supply.FarmWeight.Value > 0)
                            {
                                // Use farm weight as factory weight if farm weight exists
                                supply.FactoryWeight = supply.FarmWeight;
                                fixedCount++;
                            }
                        }
                    }

                    if (fixedCount > 0)
                    {
                        await _context.SaveChangesAsync();
                        MessageBox.Show($"تم إصلاح {fixedCount} مشكلة تلقائياً", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadData(); // Refresh data
                    }
                    else
                    {
                        MessageBox.Show("لم يتم العثور على مشاكل قابلة للإصلاح التلقائي", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في الإصلاح الشامل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "CSV Files|*.csv",
                    DefaultExt = "csv",
                    FileName = $"التوريدات_المشكوك_فيها_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCSV(saveDialog.FileName);
                    MessageBox.Show("تم تصدير البيانات بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تصدير البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCSV(string fileName)
        {
            var lines = new List<string>
            {
                "التاريخ,رقم العربية,اسم المزرعة,اسم المصنع,وزن المصنع,نوع الخطأ,وصف الخطأ,الحل المقترح,الأولوية"
            };

            foreach (var item in FilteredSupplies)
            {
                var line = $"{item.Date:dd/MM/yyyy},{item.TruckNumber},{item.FarmName},{item.FactoryName}," +
                          $"{item.FactoryWeight},{item.ErrorType},{item.ErrorDescription},{item.SuggestedFix},{item.Priority}";
                lines.Add(line);
            }

            File.WriteAllLines(fileName, lines, System.Text.Encoding.UTF8);
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

    // Model class for invalid supply items
    public class InvalidSupplyItem : INotifyPropertyChanged
    {
        private int _supplyEntryId;
        private DateTime _date;
        private string _truckNumber;
        private string _farmName;
        private string _factoryName;
        private decimal? _factoryWeight;
        private string _errorType;
        private string _errorDescription;
        private string _suggestedFix;
        private string _priority;

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

        public string TruckNumber
        {
            get => _truckNumber;
            set { _truckNumber = value; OnPropertyChanged(); }
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

        public decimal? FactoryWeight
        {
            get => _factoryWeight;
            set { _factoryWeight = value; OnPropertyChanged(); }
        }

        public string ErrorType
        {
            get => _errorType;
            set { _errorType = value; OnPropertyChanged(); }
        }

        public string ErrorDescription
        {
            get => _errorDescription;
            set { _errorDescription = value; OnPropertyChanged(); }
        }

        public string SuggestedFix
        {
            get => _suggestedFix;
            set { _suggestedFix = value; OnPropertyChanged(); }
        }

        public string Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Dialog for weight input
    public partial class WeightInputDialog : Window
    {
        public decimal Weight { get; private set; }

        public WeightInputDialog()
        {
            Title = "إدخال الوزن الصحيح";
            Width = 350;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FlowDirection = FlowDirection.RightToLeft;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock { 
                Text = "أدخل الوزن الصحيح للمصنع:", 
                Margin = new Thickness(10),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(label, 0);
            
            var textBox = new TextBox { 
                Name = "WeightTextBox", 
                Margin = new Thickness(10),
                FontSize = 14,
                Height = 30,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(textBox, 1);
            
            var buttonPanel = new StackPanel { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(10) 
            };
            var okButton = new Button { 
                Content = "موافق", 
                Width = 80, 
                Height = 30,
                Margin = new Thickness(5),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 182, 255)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            var cancelButton = new Button { 
                Content = "إلغاء", 
                Width = 80, 
                Height = 30,
                Margin = new Thickness(5),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            
            okButton.Click += (s, e) =>
            {
                if (decimal.TryParse(textBox.Text, out decimal weight) && weight > 0)
                {
                    Weight = weight;
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("يرجى إدخال وزن صحيح", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            
            cancelButton.Click += (s, e) => DialogResult = false;
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            
            grid.Children.Add(label);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }
    }

    // Dialog for factory selection
    public partial class FactorySelectionDialog : Window
    {
        public int SelectedFactoryId { get; private set; }
        private readonly FruitTrackDbContext _context;

        public FactorySelectionDialog(FruitTrackDbContext context)
        {
            _context = context;
            Title = "اختيار المصنع الصحيح";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            FlowDirection = FlowDirection.RightToLeft;
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock { 
                Text = "اختر المصنع الصحيح:", 
                Margin = new Thickness(10),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(label, 0);
            
            var listBox = new ListBox { 
                Name = "FactoryListBox", 
                Margin = new Thickness(10),
                FontSize = 14
            };
            LoadFactories(listBox);
            Grid.SetRow(listBox, 1);
            
            var buttonPanel = new StackPanel { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Center, 
                Margin = new Thickness(10) 
            };
            var okButton = new Button { 
                Content = "موافق", 
                Width = 80, 
                Height = 30,
                Margin = new Thickness(5),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(108, 182, 255)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            var cancelButton = new Button { 
                Content = "إلغاء", 
                Width = 80, 
                Height = 30,
                Margin = new Thickness(5),
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0)
            };
            
            okButton.Click += (s, e) =>
            {
                if (listBox.SelectedItem is Factory factory)
                {
                    SelectedFactoryId = factory.FactoryId;
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("يرجى اختيار مصنع", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            
            cancelButton.Click += (s, e) => DialogResult = false;
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            
            grid.Children.Add(label);
            grid.Children.Add(listBox);
            grid.Children.Add(buttonPanel);
            
            Content = grid;
        }

        private async void LoadFactories(ListBox listBox)
        {
            try
            {
                var factories = await _context.Factories.ToListAsync();
                listBox.ItemsSource = factories;
                listBox.DisplayMemberPath = "FactoryName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المصانع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 