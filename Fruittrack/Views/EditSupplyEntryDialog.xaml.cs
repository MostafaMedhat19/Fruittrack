using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Fruittrack.Models;

namespace Fruittrack.Views
{
    public partial class EditSupplyEntryDialog : Window
    {
        private readonly FruitTrackDbContext _context;
        private SupplyEntry _supplyEntry;

        public EditSupplyEntryDialog(int supplyEntryId)
        {
            InitializeComponent();
            _context = App.ServiceProvider.GetRequiredService<FruitTrackDbContext>();
            
            Loaded += async (s, e) => await EditSupplyEntryDialog_Loaded(supplyEntryId);
        }

        private async Task EditSupplyEntryDialog_Loaded(int supplyEntryId)
        {
            try
            {
                await LoadComboBoxData();
                await LoadSupplyEntry(supplyEntryId);
                
                // Ensure UI is updated after data is loaded
                await Task.Delay(200);
                Dispatcher.Invoke(() =>
                {
                    if (_supplyEntry != null)
                    {
                        PopulateFields();
                        // Force refresh of all controls
                        InvalidateVisual();
                        UpdateLayout();
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadSupplyEntry(int supplyEntryId)
        {
            try
            {
                _supplyEntry = await _context.SupplyEntries
                    .Include(s => s.Truck)
                    .Include(s => s.Farm)
                    .Include(s => s.Factory)
                    .FirstOrDefaultAsync(s => s.SupplyEntryId == supplyEntryId);

                if (_supplyEntry == null)
                {
                    MessageBox.Show("لم يتم العثور على السجل المطلوب", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async System.Threading.Tasks.Task LoadComboBoxData()
        {
            try
            {
                // Load trucks
                var trucks = await _context.Trucks.OrderBy(t => t.TruckNumber).ToListAsync();
                TruckComboBox.ItemsSource = trucks;

                // Load farms with empty option
                var farms = await _context.Farms.OrderBy(f => f.FarmName).ToListAsync();
                var farmsWithEmpty = new List<Farm> { new Farm { FarmId = 0, FarmName = "-- اختر المزرعة --" } };
                farmsWithEmpty.AddRange(farms);
                FarmComboBox.ItemsSource = farmsWithEmpty;

                // Load factories with empty option
                var factories = await _context.Factories.OrderBy(f => f.FactoryName).ToListAsync();
                var factoriesWithEmpty = new List<Factory> { new Factory { FactoryId = 0, FactoryName = "-- اختر المصنع --" } };
                factoriesWithEmpty.AddRange(factories);
                FactoryComboBox.ItemsSource = factoriesWithEmpty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateFields()
        {
            if (_supplyEntry == null) return;

            try
            {
                // Show debug information
                System.Diagnostics.Debug.WriteLine($"Populating fields for SupplyEntry ID: {_supplyEntry.SupplyEntryId}");
                System.Diagnostics.Debug.WriteLine($"Farm Weight: {_supplyEntry.FarmWeight}");
                System.Diagnostics.Debug.WriteLine($"Factory Weight: {_supplyEntry.FactoryWeight}");

                // Basic info
                EntryDatePicker.SelectedDate = _supplyEntry.EntryDate;
                TruckComboBox.SelectedValue = _supplyEntry.TruckId;

                // Farm info
                if (_supplyEntry.FarmId.HasValue && _supplyEntry.FarmId.Value > 0)
                {
                    FarmComboBox.SelectedValue = _supplyEntry.FarmId.Value;
                }
                else
                {
                    FarmComboBox.SelectedIndex = 0; // Select "-- اختر المزرعة --"
                }

                FarmWeightTextBox.Text = _supplyEntry.FarmWeight?.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) ?? "";
                FarmDiscountTextBox.Text = _supplyEntry.FarmDiscountRate?.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) ?? "";
                FarmPriceTextBox.Text = _supplyEntry.FarmPricePerKilo?.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) ?? "";

                // Factory info
                if (_supplyEntry.FactoryId.HasValue && _supplyEntry.FactoryId.Value > 0)
                {
                    FactoryComboBox.SelectedValue = _supplyEntry.FactoryId.Value;
                }
                else
                {
                    FactoryComboBox.SelectedIndex = 0; // Select "-- اختر المصنع --"
                }

                FactoryWeightTextBox.Text = _supplyEntry.FactoryWeight?.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) ?? "";
                FactoryDiscountTextBox.Text = _supplyEntry.FactoryDiscountRate?.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) ?? "";
                FactoryPriceTextBox.Text = _supplyEntry.FactoryPricePerKilo?.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) ?? "";

                // Transport and notes
                FreightCostTextBox.Text = _supplyEntry.FreightCost?.ToString("F0", System.Globalization.CultureInfo.InvariantCulture) ?? "";
                NotesTextBox.Text = _supplyEntry.Notes ?? "";

                // Debug - verify text box values
                System.Diagnostics.Debug.WriteLine($"FarmWeightTextBox.Text set to: '{FarmWeightTextBox.Text}'");
                System.Diagnostics.Debug.WriteLine($"FactoryWeightTextBox.Text set to: '{FactoryWeightTextBox.Text}'");

                // Force all controls to update their display
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    foreach (var textBox in FindVisualChildren<TextBox>(this))
                    {
                        textBox.InvalidateVisual();
                        textBox.UpdateLayout();
                    }
                    
                    foreach (var comboBox in FindVisualChildren<ComboBox>(this))
                    {
                        comboBox.InvalidateVisual();
                        comboBox.UpdateLayout();
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);

                // Force focus to first field to ensure UI is ready
                EntryDatePicker.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تعبئة الحقول: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                // Update basic info
                if (EntryDatePicker.SelectedDate.HasValue)
                    _supplyEntry.EntryDate = EntryDatePicker.SelectedDate.Value;

                if (TruckComboBox.SelectedValue != null)
                    _supplyEntry.TruckId = (int)TruckComboBox.SelectedValue;

                // Update farm info
                var farmId = (int?)FarmComboBox.SelectedValue;
                _supplyEntry.FarmId = farmId == 0 ? null : farmId;

                _supplyEntry.FarmWeight = ParseDecimalOrNull(FarmWeightTextBox.Text);
                _supplyEntry.FarmDiscountRate = ParseDecimalOrNull(FarmDiscountTextBox.Text);
                _supplyEntry.FarmPricePerKilo = ParseDecimalOrNull(FarmPriceTextBox.Text);

                // Update factory info
                var factoryId = (int?)FactoryComboBox.SelectedValue;
                _supplyEntry.FactoryId = factoryId == 0 ? null : factoryId;

                _supplyEntry.FactoryWeight = ParseDecimalOrNull(FactoryWeightTextBox.Text);
                _supplyEntry.FactoryDiscountRate = ParseDecimalOrNull(FactoryDiscountTextBox.Text);
                _supplyEntry.FactoryPricePerKilo = ParseDecimalOrNull(FactoryPriceTextBox.Text);

                // Update transport and notes
                _supplyEntry.FreightCost = ParseDecimalOrNull(FreightCostTextBox.Text);
                _supplyEntry.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

                // Save to database
                await _context.SaveChangesAsync();

                MessageBox.Show("تم حفظ التغييرات بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                base.DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            base.DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            var errors = new List<string>();

            // Validate date
            if (!EntryDatePicker.SelectedDate.HasValue)
                errors.Add("يجب اختيار تاريخ التوريد");

            // Validate truck
            if (TruckComboBox.SelectedValue == null)
                errors.Add("يجب اختيار رقم العربية");

            // Validate numeric fields
            if (!string.IsNullOrWhiteSpace(FarmWeightTextBox.Text) && ParseDecimalOrNull(FarmWeightTextBox.Text) == null)
                errors.Add("وزن المزرعة يجب أن يكون رقماً صحيحاً");

            if (!string.IsNullOrWhiteSpace(FarmDiscountTextBox.Text) && ParseDecimalOrNull(FarmDiscountTextBox.Text) == null)
                errors.Add("خصم المزرعة يجب أن يكون رقماً صحيحاً");

            if (!string.IsNullOrWhiteSpace(FarmPriceTextBox.Text) && ParseDecimalOrNull(FarmPriceTextBox.Text) == null)
                errors.Add("سعر المزرعة يجب أن يكون رقماً صحيحاً");

            if (!string.IsNullOrWhiteSpace(FactoryWeightTextBox.Text) && ParseDecimalOrNull(FactoryWeightTextBox.Text) == null)
                errors.Add("وزن المصنع يجب أن يكون رقماً صحيحاً");

            if (!string.IsNullOrWhiteSpace(FactoryDiscountTextBox.Text) && ParseDecimalOrNull(FactoryDiscountTextBox.Text) == null)
                errors.Add("خصم المصنع يجب أن يكون رقماً صحيحاً");

            if (!string.IsNullOrWhiteSpace(FactoryPriceTextBox.Text) && ParseDecimalOrNull(FactoryPriceTextBox.Text) == null)
                errors.Add("سعر المصنع يجب أن يكون رقماً صحيحاً");

            if (!string.IsNullOrWhiteSpace(FreightCostTextBox.Text) && ParseDecimalOrNull(FreightCostTextBox.Text) == null)
                errors.Add("تكلفة النولون يجب أن تكون رقماً صحيحاً");

            // Validate ranges
            var farmDiscount = ParseDecimalOrNull(FarmDiscountTextBox.Text);
            if (farmDiscount.HasValue && (farmDiscount < 0 || farmDiscount > 100))
                errors.Add("خصم المزرعة يجب أن يكون بين 0 و 100");

            var factoryDiscount = ParseDecimalOrNull(FactoryDiscountTextBox.Text);
            if (factoryDiscount.HasValue && (factoryDiscount < 0 || factoryDiscount > 100))
                errors.Add("خصم المصنع يجب أن يكون بين 0 و 100");

            if (errors.Any())
            {
                string errorMessage = "يرجى تصحيح الأخطاء التالية:\n\n" + string.Join("\n", errors);
                MessageBox.Show(errorMessage, "خطأ في البيانات", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private decimal? ParseDecimalOrNull(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            
            string cleanText = text.Trim();
            
            // Try parsing with current culture first
            if (decimal.TryParse(cleanText, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal result))
                return result;
            
            // Try parsing with invariant culture (for English numbers)
            if (decimal.TryParse(cleanText, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
                return result;
            
            // Try parsing with dot as decimal separator
            if (decimal.TryParse(cleanText.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out result))
                return result;
            
            return null;
        }

        // Helper method to find all children of a specific type
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    yield return typedChild;
                }

                foreach (var descendant in FindVisualChildren<T>(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}