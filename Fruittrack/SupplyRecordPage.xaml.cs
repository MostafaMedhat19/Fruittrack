using Fruittrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Fruittrack
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public static readonly BooleanToVisibilityConverter Instance = new BooleanToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value == Visibility.Visible;
        }
    }

    public partial class SupplyRecordPage : Page, INotifyPropertyChanged
    {
        // ViewModel for binding
        public class SupplyRecordViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            // Dropdown data
            public ObservableCollection<Farm> Farms { get; set; } = new();
            public ObservableCollection<Factory> Factories { get; set; } = new();

            // User input properties
            public DateTime? Date { get => _date; set { _date = value; OnPropertyChanged(nameof(Date)); } }
            private DateTime? _date = DateTime.Today; // Initialize to today but allow editing



            public string TruckNumber { get => _truckNumber; set { _truckNumber = value; OnPropertyChanged(nameof(TruckNumber)); ValidateTruckNumber(); } }
            private string _truckNumber = string.Empty;

            public decimal? TransportPrice { get => _transportPrice; set { _transportPrice = value; OnPropertyChanged(nameof(TransportPrice)); ValidateTransportPrice(); UpdateProfit(); } }
            private decimal? _transportPrice;

            public Farm SelectedFarm { get => _selectedFarm; set { _selectedFarm = value; OnPropertyChanged(nameof(SelectedFarm)); FarmId = value?.FarmId; ValidateFarmName(); } }
            private Farm _selectedFarm;

            public int? FarmId { get => _farmId; set { _farmId = value; OnPropertyChanged(nameof(FarmId)); } }
            private int? _farmId;

            public decimal? FarmWeight { get => _farmWeight; set { _farmWeight = value; OnPropertyChanged(nameof(FarmWeight)); ValidateFarmWeight(); UpdateFarmAllowedWeight(); } }
            private decimal? _farmWeight;

            public decimal? FarmDiscountPercentage { get => _farmDiscountPercentage; set { _farmDiscountPercentage = value; OnPropertyChanged(nameof(FarmDiscountPercentage)); ValidateFarmDiscount(); UpdateFarmAllowedWeight(); } }
            private decimal? _farmDiscountPercentage;

            public decimal? FarmAllowedWeight { get => _farmAllowedWeight; set { _farmAllowedWeight = value; OnPropertyChanged(nameof(FarmAllowedWeight)); UpdateFarmTotal(); } }
            private decimal? _farmAllowedWeight;

            public decimal? FarmPricePerTon { get => _farmPricePerTon; set { _farmPricePerTon = value; OnPropertyChanged(nameof(FarmPricePerTon)); ValidateFarmPrice(); UpdateFarmTotal(); } }
            private decimal? _farmPricePerTon;

            public decimal? FarmTotal { get => _farmTotal; set { _farmTotal = value; OnPropertyChanged(nameof(FarmTotal)); UpdateProfit(); } }
            private decimal? _farmTotal;

            public Factory SelectedFactory { get => _selectedFactory; set { _selectedFactory = value; OnPropertyChanged(nameof(SelectedFactory)); FactoryId = value?.FactoryId; ValidateFactoryName(); } }
            private Factory _selectedFactory;

            public int? FactoryId { get => _factoryId; set { _factoryId = value; OnPropertyChanged(nameof(FactoryId)); } }
            private int? _factoryId;

            public decimal? FactoryWeight { get => _factoryWeight; set { _factoryWeight = value; OnPropertyChanged(nameof(FactoryWeight)); ValidateFactoryWeight(); UpdateFactoryAllowedWeight(); } }
            private decimal? _factoryWeight;

            public decimal? FactoryDiscountPercentage { get => _factoryDiscountPercentage; set { _factoryDiscountPercentage = value; OnPropertyChanged(nameof(FactoryDiscountPercentage)); ValidateFactoryDiscount(); UpdateFactoryAllowedWeight(); } }
            private decimal? _factoryDiscountPercentage;

            public decimal? FactoryAllowedWeight { get => _factoryAllowedWeight; set { _factoryAllowedWeight = value; OnPropertyChanged(nameof(FactoryAllowedWeight)); UpdateFactoryTotal(); } }
            private decimal? _factoryAllowedWeight;

            public decimal? FactoryPricePerTon { get => _factoryPricePerTon; set { _factoryPricePerTon = value; OnPropertyChanged(nameof(FactoryPricePerTon)); ValidateFactoryPrice(); UpdateFactoryTotal(); } }
            private decimal? _factoryPricePerTon;

            public decimal? FactoryTotal { get => _factoryTotal; set { _factoryTotal = value; OnPropertyChanged(nameof(FactoryTotal)); UpdateProfit(); } }
            private decimal? _factoryTotal;

            public decimal? ProfitMargin { get => _profitMargin; set { _profitMargin = value; OnPropertyChanged(nameof(ProfitMargin)); } }
            private decimal? _profitMargin;

            public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(nameof(Notes)); } }
            private string _notes;

            // Error message properties
            public string TruckNumberError { get => _truckNumberError; set { _truckNumberError = value; OnPropertyChanged(nameof(TruckNumberError)); OnPropertyChanged(nameof(HasTruckNumberError)); } }
            private string _truckNumberError;
            public bool HasTruckNumberError => !string.IsNullOrEmpty(TruckNumberError);

            public string TransportPriceError { get => _transportPriceError; set { _transportPriceError = value; OnPropertyChanged(nameof(TransportPriceError)); OnPropertyChanged(nameof(HasTransportPriceError)); } }
            private string _transportPriceError;
            public bool HasTransportPriceError => !string.IsNullOrEmpty(TransportPriceError);

            public string FarmNameError { get => _farmNameError; set { _farmNameError = value; OnPropertyChanged(nameof(FarmNameError)); OnPropertyChanged(nameof(HasFarmNameError)); } }
            private string _farmNameError;
            public bool HasFarmNameError => !string.IsNullOrEmpty(FarmNameError);

            public string FarmWeightError { get => _farmWeightError; set { _farmWeightError = value; OnPropertyChanged(nameof(FarmWeightError)); OnPropertyChanged(nameof(HasFarmWeightError)); } }
            private string _farmWeightError;
            public bool HasFarmWeightError => !string.IsNullOrEmpty(FarmWeightError);

            public string FarmDiscountError { get => _farmDiscountError; set { _farmDiscountError = value; OnPropertyChanged(nameof(FarmDiscountError)); OnPropertyChanged(nameof(HasFarmDiscountError)); } }
            private string _farmDiscountError;
            public bool HasFarmDiscountError => !string.IsNullOrEmpty(FarmDiscountError);

            public string FarmPriceError { get => _farmPriceError; set { _farmPriceError = value; OnPropertyChanged(nameof(FarmPriceError)); OnPropertyChanged(nameof(HasFarmPriceError)); } }
            private string _farmPriceError;
            public bool HasFarmPriceError => !string.IsNullOrEmpty(FarmPriceError);

            public string FactoryNameError { get => _factoryNameError; set { _factoryNameError = value; OnPropertyChanged(nameof(FactoryNameError)); OnPropertyChanged(nameof(HasFactoryNameError)); } }
            private string _factoryNameError;
            public bool HasFactoryNameError => !string.IsNullOrEmpty(FactoryNameError);

            public string FactoryWeightError { get => _factoryWeightError; set { _factoryWeightError = value; OnPropertyChanged(nameof(FactoryWeightError)); OnPropertyChanged(nameof(HasFactoryWeightError)); } }
            private string _factoryWeightError;
            public bool HasFactoryWeightError => !string.IsNullOrEmpty(FactoryWeightError);

            public string FactoryDiscountError { get => _factoryDiscountError; set { _factoryDiscountError = value; OnPropertyChanged(nameof(FactoryDiscountError)); OnPropertyChanged(nameof(HasFactoryDiscountError)); } }
            private string _factoryDiscountError;
            public bool HasFactoryDiscountError => !string.IsNullOrEmpty(FactoryDiscountError);

            public string FactoryPriceError { get => _factoryPriceError; set { _factoryPriceError = value; OnPropertyChanged(nameof(FactoryPriceError)); OnPropertyChanged(nameof(HasFactoryPriceError)); } }
            private string _factoryPriceError;
            public bool HasFactoryPriceError => !string.IsNullOrEmpty(FactoryPriceError);



            // Calculation methods
            private void UpdateFarmAllowedWeight()
            {
                if (FarmWeight.HasValue && FarmDiscountPercentage.HasValue)
                    FarmAllowedWeight = FarmWeight.Value * (1 - (FarmDiscountPercentage.Value / 100m));
                else
                    FarmAllowedWeight = null;
            }
            private void UpdateFarmTotal()
            {
                if (FarmAllowedWeight.HasValue && FarmPricePerTon.HasValue)
                    FarmTotal = FarmAllowedWeight.Value * FarmPricePerTon.Value;
                else
                    FarmTotal = null;
            }
            private void UpdateFactoryAllowedWeight()
            {
                if (FactoryWeight.HasValue && FactoryDiscountPercentage.HasValue)
                    FactoryAllowedWeight = FactoryWeight.Value * (1 - (FactoryDiscountPercentage.Value / 100m));
                else
                    FactoryAllowedWeight = null;
            }
            private void UpdateFactoryTotal()
            {
                if (FactoryAllowedWeight.HasValue && FactoryPricePerTon.HasValue)
                    FactoryTotal = FactoryAllowedWeight.Value * FactoryPricePerTon.Value;
                else
                    FactoryTotal = null;
            }
            private void UpdateProfit()
            {
                if (FactoryTotal.HasValue && FarmTotal.HasValue && TransportPrice.HasValue)
                    ProfitMargin = FactoryTotal.Value - (FarmTotal.Value + TransportPrice.Value);
                else
                    ProfitMargin = null;
            }

            // Validation methods - REQUIRED: Date & Truck Number only!
            private void ValidateTruckNumber()
            {
                // REQUIRED FIELD - Always show error if empty
                TruckNumberError = string.IsNullOrWhiteSpace(TruckNumber) ? "يرجى إدخال رقم العربية" : "";
            }

            private void ValidateTransportPrice()
            {
                // OPTIONAL FIELD - Only validate format if user entered something
                if (!TransportPrice.HasValue)
                    TransportPriceError = ""; // No error if empty
                else if (TransportPrice <= 0)
                    TransportPriceError = "سعر النقل يجب أن يكون أكبر من صفر";
                else
                    TransportPriceError = "";
            }

            private void ValidateFarmName()
            {
                // OPTIONAL FIELD - No error if empty
                FarmNameError = "";
            }

            private void ValidateFarmWeight()
            {
                // OPTIONAL FIELD - Only validate format if user entered something
                if (!FarmWeight.HasValue)
                    FarmWeightError = ""; // No error if empty  
                else if (FarmWeight <= 0)
                    FarmWeightError = "الوزن يجب أن يكون أكبر من صفر";
                else
                    FarmWeightError = "";
            }

            private void ValidateFarmDiscount()
            {
                // OPTIONAL FIELD - Only validate format if user entered something
                if (!FarmDiscountPercentage.HasValue)
                    FarmDiscountError = ""; // No error if empty
                else if (FarmDiscountPercentage < 0 || FarmDiscountPercentage > 100)
                    FarmDiscountError = "نسبة الخصم يجب أن تكون بين 0 و 100";
                else
                    FarmDiscountError = "";
            }

            private void ValidateFarmPrice()
            {
                // OPTIONAL FIELD - Only validate format if user entered something
                if (!FarmPricePerTon.HasValue)
                    FarmPriceError = ""; // No error if empty
                else if (FarmPricePerTon <= 0)
                    FarmPriceError = "السعر يجب أن يكون أكبر من صفر";
                else
                    FarmPriceError = "";
            }

            private void ValidateFactoryName()
            {
                // OPTIONAL FIELD - No error if empty
                FactoryNameError = "";
            }

            private void ValidateFactoryWeight()
            {
                // OPTIONAL FIELD - Only validate format if user entered something
                if (!FactoryWeight.HasValue)
                    FactoryWeightError = ""; // No error if empty
                else if (FactoryWeight <= 0)
                    FactoryWeightError = "الوزن يجب أن يكون أكبر من صفر";
                else
                    FactoryWeightError = "";
            }

            private void ValidateFactoryDiscount()
            {
                // OPTIONAL FIELD - Only validate format if user entered something
                if (!FactoryDiscountPercentage.HasValue)
                    FactoryDiscountError = ""; // No error if empty
                else if (FactoryDiscountPercentage < 0 || FactoryDiscountPercentage > 100)
                    FactoryDiscountError = "نسبة الخصم يجب أن تكون بين 0 و 100";
                else
                    FactoryDiscountError = "";
            }

            private void ValidateFactoryPrice()
            {
                // OPTIONAL FIELD - Only validate format if user entered something
                if (!FactoryPricePerTon.HasValue)
                    FactoryPriceError = ""; // No error if empty
                else if (FactoryPricePerTon <= 0)
                    FactoryPriceError = "السعر يجب أن يكون أكبر من صفر";
                else
                    FactoryPriceError = "";
            }

            public bool ValidateAll()
            {
                // REQUIRED validations - only TruckNumber (Date is always set by DatePicker)
                ValidateTruckNumber();
                
                // OPTIONAL validations - check format only if user entered data
                ValidateTransportPrice();
                ValidateFarmName();
                ValidateFarmWeight();
                ValidateFarmDiscount();
                ValidateFarmPrice();
                ValidateFactoryName();
                ValidateFactoryWeight();
                ValidateFactoryDiscount();
                ValidateFactoryPrice();

                // Only fail if REQUIRED fields have errors OR optional fields have format errors
                return !HasTruckNumberError && !HasTransportPriceError && !HasFarmNameError && 
                       !HasFarmWeightError && !HasFarmDiscountError && !HasFarmPriceError &&
                       !HasFactoryNameError && !HasFactoryWeightError && !HasFactoryDiscountError && !HasFactoryPriceError;
            }
        }

        public SupplyRecordViewModel ViewModel { get; set; } = new();
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private readonly FruitTrackDbContext _context;

                public SupplyRecordPage()
        {
            InitializeComponent();

            // خزن نسخة واحدة من الكونتكست
            _context = App.ServiceProvider.GetRequiredService<FruitTrackDbContext>();

            DataContext = ViewModel;
            Loaded += SupplyRecordPage_Loaded;
            btnSave.Click += BtnSave_Click;
            btnClear.Click += BtnClear_Click;
            btnClose.Click += (s, e) => ClosePage();
            btnCloseBottom.Click += (s, e) => ClosePage();
        }

        private void CalendarControl_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var datePicker = sender as DatePicker;
            if (datePicker?.SelectedDate.HasValue == true)
            {
                ViewModel.Date = datePicker.SelectedDate.Value;
                // Force UI update
                datePicker.UpdateLayout();
            }
        }




        private void SupplyRecordPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Populate dropdowns
            var context = _context;
            ViewModel.Farms.Clear();
            foreach (var farm in context.Farms.ToList())
            {
                ViewModel.Farms.Add(farm);
            }

            ViewModel.Factories.Clear();
            foreach (var factory in context.Factories.ToList())
            {
                ViewModel.Factories.Add(factory);
            }

            // Wire up ComboBox text change events
            FarmName.LostFocus += FarmName_LostFocus;
            FactoryName.LostFocus += FactoryName_LostFocus;
        }

        private void FarmName_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                if (!string.IsNullOrWhiteSpace(comboBox.Text))
                {
                    // User entered farm name
                    var existingFarm = ViewModel.Farms.FirstOrDefault(f => f.FarmName.Equals(comboBox.Text, StringComparison.OrdinalIgnoreCase));
                    if (existingFarm == null)
                    {
                        // Create new farm
                        var newFarm = new Farm { FarmId = 0, FarmName = comboBox.Text.Trim() };
                        ViewModel.Farms.Add(newFarm);
                        ViewModel.SelectedFarm = newFarm;
                    }
                    else
                    {
                        ViewModel.SelectedFarm = existingFarm;
                    }
                }
                else
                {
                    // User cleared the farm name - set to null (optional)
                    ViewModel.SelectedFarm = null;
                }
            }
        }

        private void FactoryName_LostFocus(object sender, RoutedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                if (!string.IsNullOrWhiteSpace(comboBox.Text))
                {
                    // User entered factory name
                    var existingFactory = ViewModel.Factories.FirstOrDefault(f => f.FactoryName.Equals(comboBox.Text, StringComparison.OrdinalIgnoreCase));
                    if (existingFactory == null)
                    {
                        // Create new factory
                        var newFactory = new Factory { FactoryId = 0, FactoryName = comboBox.Text.Trim() };
                        ViewModel.Factories.Add(newFactory);
                        ViewModel.SelectedFactory = newFactory;
                    }
                    else
                    {
                        ViewModel.SelectedFactory = existingFactory;
                    }
                }
                else
                {
                    // User cleared the factory name - set to null (optional)
                    ViewModel.SelectedFactory = null;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate using the new validation system
            if (!ViewModel.ValidateAll())
            {
                return; // Errors are already displayed
            }

            try
            {
                var context = _context;
                
                // Handle OPTIONAL Farm (only if user entered data)
                Farm? farm = null;
                if (ViewModel.SelectedFarm != null && !string.IsNullOrWhiteSpace(ViewModel.SelectedFarm.FarmName))
                {
                    farm = ViewModel.SelectedFarm;
                    if (farm.FarmId == 0) // New farm
                    {
                        context.Farms.Add(farm);
                        context.SaveChanges();
                    }
                }

                // Handle OPTIONAL Factory (only if user entered data)
                Factory? factory = null;
                if (ViewModel.SelectedFactory != null && !string.IsNullOrWhiteSpace(ViewModel.SelectedFactory.FactoryName))
                {
                    factory = ViewModel.SelectedFactory;
                    if (factory.FactoryId == 0) // New factory
                    {
                        context.Factories.Add(factory);
                        context.SaveChanges();
                    }
                }

                // REQUIRED Truck: find or create
                var truck = context.Trucks.FirstOrDefault(t => t.TruckNumber == ViewModel.TruckNumber);
                if (truck == null)
                {
                    truck = new Truck { TruckNumber = ViewModel.TruckNumber };
                    context.Trucks.Add(truck);
                    context.SaveChanges();
                }

                // Create SupplyEntry with OPTIONAL fields properly handled
                var supplyEntry = new SupplyEntry
                {
                    // REQUIRED FIELDS 
                    EntryDate = ViewModel.Date ?? DateTime.Today,
                    TruckId = truck.TruckId,
                    
                    // OPTIONAL FIELDS - only set if user provided data and farm exists
                    FarmId = farm?.FarmId,
                    FarmWeight = farm != null ? ViewModel.FarmWeight : null,
                    FarmDiscountRate = farm != null ? ViewModel.FarmDiscountPercentage : null,
                    FarmPricePerTon = farm != null ? ViewModel.FarmPricePerTon : null,
                    
                    // OPTIONAL FIELDS - only set if user provided data and factory exists
                    FactoryId = factory?.FactoryId,
                    FactoryWeight = factory != null ? ViewModel.FactoryWeight : null,
                    FactoryDiscountRate = factory != null ? ViewModel.FactoryDiscountPercentage : null,
                    FactoryPricePerTon = factory != null ? ViewModel.FactoryPricePerTon : null,
                    
                    // OPTIONAL TRANSPORT
                    FreightCost = ViewModel.TransportPrice,
                    TransferFrom = farm?.FarmName,
                    TransferTo = factory?.FactoryName,
                    
                    // OPTIONAL NOTES
                    Notes = ViewModel.Notes,
                };

                context.SupplyEntries.Add(supplyEntry);
                context.SaveChanges();
                
                MessageBox.Show("تم حفظ بيانات التوريد بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                // Show detailed error information
                string errorMessage = $"حدث خطأ أثناء الحفظ:\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nتفاصيل الخطأ:\n{ex.InnerException.Message}";
                }
                MessageBox.Show(errorMessage, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnClear_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            ViewModel.Date = DateTime.Today;
            ViewModel.TruckNumber = string.Empty;
            ViewModel.TransportPrice = null;
            ViewModel.SelectedFarm = null;
            ViewModel.FarmId = null;
            ViewModel.FarmWeight = null;
            ViewModel.FarmDiscountPercentage = null;
            ViewModel.FarmAllowedWeight = null;
            ViewModel.FarmPricePerTon = null;
            ViewModel.FarmTotal = null;
            ViewModel.SelectedFactory = null;
            ViewModel.FactoryId = null;
            ViewModel.FactoryWeight = null;
            ViewModel.FactoryDiscountPercentage = null;
            ViewModel.FactoryAllowedWeight = null;
            ViewModel.FactoryPricePerTon = null;
            ViewModel.FactoryTotal = null;
            ViewModel.ProfitMargin = null;
            ViewModel.Notes = string.Empty;

            ViewModel.TruckNumberError = string.Empty;
            ViewModel.TransportPriceError = string.Empty;
            ViewModel.FarmNameError = string.Empty;
            ViewModel.FarmWeightError = string.Empty;
            ViewModel.FarmDiscountError = string.Empty;
            ViewModel.FarmPriceError = string.Empty;
            ViewModel.FactoryNameError = string.Empty;
            ViewModel.FactoryWeightError = string.Empty;
            ViewModel.FactoryDiscountError = string.Empty;
            ViewModel.FactoryPriceError = string.Empty;
        }



        private void ClosePage()
        {
            // If using navigation, go back
            if (NavigationService != null && NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                Window.GetWindow(this)?.Close();
        }

     
    }
}
