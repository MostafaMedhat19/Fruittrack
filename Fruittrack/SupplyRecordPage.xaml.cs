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
    /// <summary>
    /// Interaction logic for SupplyRecordPage.xaml
    /// </summary>
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

            // Text representation to preserve exact user input for transport price
            public string TransportPriceText
            {
                get => _transportPriceText;
                set
                {
                    _transportPriceText = value;
                    OnPropertyChanged(nameof(TransportPriceText));
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        TransportPrice = null;
                    }
                    else if (TryParseDecimalFlexible(value, out var parsed))
                    {
                        TransportPrice = parsed;
                    }
                    else
                    {
                        // Do not change TransportPrice on invalid text; validation will surface
                        TransportPrice = null;
                    }
                    ValidateTransportPrice();
                    UpdateProfit();
                }
            }
            private string _transportPriceText = string.Empty;

            public decimal? TransportPrice { get => _transportPrice; set { _transportPrice = value; OnPropertyChanged(nameof(TransportPrice)); } }
            private decimal? _transportPrice;

            public Farm? SelectedFarm { get => _selectedFarm; set { _selectedFarm = value; OnPropertyChanged(nameof(SelectedFarm)); FarmId = value?.FarmId; ValidateFarmName(); } }
            private Farm? _selectedFarm;

            public int? FarmId { get => _farmId; set { _farmId = value; OnPropertyChanged(nameof(FarmId)); } }
            private int? _farmId;

            public decimal? FarmWeight { get => _farmWeight; set { _farmWeight = value; OnPropertyChanged(nameof(FarmWeight)); ValidateFarmWeight(); UpdateFarmAllowedWeight(); } }
            private decimal? _farmWeight;

            public decimal? FarmDiscountPercentage { get => _farmDiscountPercentage; set { _farmDiscountPercentage = value; OnPropertyChanged(nameof(FarmDiscountPercentage)); ValidateFarmDiscount(); UpdateFarmAllowedWeight(); } }
            private decimal? _farmDiscountPercentage;

            public decimal? FarmAllowedWeight { get => _farmAllowedWeight; set { _farmAllowedWeight = value; OnPropertyChanged(nameof(FarmAllowedWeight)); UpdateFarmTotal(); } }
            private decimal? _farmAllowedWeight;

            // Text representation to preserve exact user input for farm price
            public string FarmPriceText
            {
                get => _farmPriceText;
                set
                {
                    _farmPriceText = value;
                    OnPropertyChanged(nameof(FarmPriceText));
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        FarmPrice = null;
                    }
                    else if (TryParseDecimalFlexible(value, out var parsed))
                    {
                        FarmPrice = parsed;
                    }
                    else
                    {
                        FarmPrice = null;
                    }
                    ValidateFarmPrice();
                    UpdateFarmTotal();
                }
            }
            private string _farmPriceText = string.Empty;

            public decimal? FarmPrice { get => _farmPricePerKilo; set { _farmPricePerKilo = value; OnPropertyChanged(nameof(FarmPrice)); } }
            private decimal? _farmPricePerKilo;

            public decimal? FarmTotal { get => _farmTotal; set { _farmTotal = value; OnPropertyChanged(nameof(FarmTotal)); UpdateProfit(); } }
            private decimal? _farmTotal;

            public Factory? SelectedFactory { get => _selectedFactory; set { _selectedFactory = value; OnPropertyChanged(nameof(SelectedFactory)); ValidateFactoryName(); } }
            private Factory? _selectedFactory;

            public int? FactoryId { get => _factoryId; set { _factoryId = value; OnPropertyChanged(nameof(FactoryId)); } }
            private int? _factoryId;

            public decimal? FactoryWeight { get => _factoryWeight; set { _factoryWeight = value; OnPropertyChanged(nameof(FactoryWeight)); ValidateFactoryWeight(); UpdateFactoryAllowedWeight(); } }
            private decimal? _factoryWeight;

            public decimal? FactoryDiscount { get => _factoryDiscountPercentage; set { _factoryDiscountPercentage = value; OnPropertyChanged(nameof(FactoryDiscount)); ValidateFactoryDiscount(); UpdateFactoryAllowedWeight(); } }
            private decimal? _factoryDiscountPercentage;

            public decimal? FactoryAllowedWeight { get => _factoryAllowedWeight; set { _factoryAllowedWeight = value; OnPropertyChanged(nameof(FactoryAllowedWeight)); UpdateFactoryTotal(); } }
            private decimal? _factoryAllowedWeight;

            // Text representation to preserve exact user input for factory price
            public string FactoryPriceText
            {
                get => _factoryPriceText;
                set
                {
                    _factoryPriceText = value;
                    OnPropertyChanged(nameof(FactoryPriceText));
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        FactoryPrice = null;
                    }
                    else if (TryParseDecimalFlexible(value, out var parsed))
                    {
                        FactoryPrice = parsed;
                    }
                    else
                    {
                        FactoryPrice = null;
                    }
                    ValidateFactoryPrice();
                    UpdateFactoryTotal();
                }
            }
            private string _factoryPriceText = string.Empty;

            public decimal? FactoryPrice { get => _factoryPricePerKilo; set { _factoryPricePerKilo = value; OnPropertyChanged(nameof(FactoryPrice)); } }
            private decimal? _factoryPricePerKilo;

            public decimal? FactoryTotal { get => _factoryTotal; set { _factoryTotal = value; OnPropertyChanged(nameof(FactoryTotal)); UpdateProfit(); } }
            private decimal? _factoryTotal;

            public decimal? ProfitMargin { get => _profitMargin; set { _profitMargin = value; OnPropertyChanged(nameof(ProfitMargin)); } }
            private decimal? _profitMargin;

            public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(nameof(Notes)); } }
            private string _notes;

            // New: Transport contractor name (cash disbursement party)
            public string TransportContractorName { get => _transportContractorName; set { _transportContractorName = value; OnPropertyChanged(nameof(TransportContractorName)); } }
            private string _transportContractorName = string.Empty;

            // Farm Special Data Number (for cash disbursement validation)
            public string FarmSpecialDataNumberText
            {
                get => _farmSpecialDataNumberText;
                set
                {
                    _farmSpecialDataNumberText = value;
                    OnPropertyChanged(nameof(FarmSpecialDataNumberText));
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        FarmSpecialDataNumber = null;
                    }
                    else if (TryParseDecimalFlexible(value, out var parsed))
                    {
                        FarmSpecialDataNumber = parsed;
                    }
                    else
                    {
                        FarmSpecialDataNumber = null;
                    }
                    ValidateFarmSpecialData();
                }
            }
            private string _farmSpecialDataNumberText = string.Empty;

            public decimal? FarmSpecialDataNumber { get => _farmSpecialDataNumber; set { _farmSpecialDataNumber = value; OnPropertyChanged(nameof(FarmSpecialDataNumber)); } }
            private decimal? _farmSpecialDataNumber;

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

            public string FarmSpecialDataError { get => _farmSpecialDataError; set { _farmSpecialDataError = value; OnPropertyChanged(nameof(FarmSpecialDataError)); OnPropertyChanged(nameof(HasFarmSpecialDataError)); } }
            private string _farmSpecialDataError;
            public bool HasFarmSpecialDataError => !string.IsNullOrEmpty(FarmSpecialDataError);

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
                if (FarmWeight.HasValue)
                {
                    if (FarmDiscountPercentage.HasValue)
                    {
                        // Calculate with discount
                        FarmAllowedWeight = FarmWeight.Value * (1 - (FarmDiscountPercentage.Value / 100m));
                    }
                    else
                    {
                        // No discount applied, allowed weight equals farm weight
                        FarmAllowedWeight = FarmWeight.Value;
                    }
                }
                else
                {
                    FarmAllowedWeight = null;
                }
            }
            private void UpdateFarmTotal()
            {
                // Calculate total using allowed weight (discount already applied in FarmAllowedWeight)
                if (FarmAllowedWeight.HasValue && FarmPrice.HasValue)
                    FarmTotal = FarmAllowedWeight.Value * FarmPrice.Value;
                else
                    FarmTotal = 0;
            }
            private void UpdateFactoryAllowedWeight()
            {
                if (FactoryWeight.HasValue)
                {
                    if (FactoryDiscount.HasValue)
                    {
                        // Calculate with discount
                        FactoryAllowedWeight = FactoryWeight.Value * (1 - (FactoryDiscount.Value / 100m));
                    }
                    else
                    {
                        // No discount applied, allowed weight equals factory weight
                        FactoryAllowedWeight = FactoryWeight.Value;
                    }
                }
                else
                {
                    FactoryAllowedWeight = null;
                }

                UpdateFactoryTotal();
            }

            private void UpdateFactoryTotal()
            {
                // Calculate total using allowed weight (discount already applied in FactoryAllowedWeight)
                if (FactoryAllowedWeight.HasValue && FactoryPrice.HasValue)
                    FactoryTotal = FactoryAllowedWeight.Value * FactoryPrice.Value;
                else
                    FactoryTotal = 0;
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
                if (string.IsNullOrWhiteSpace(TransportPriceText))
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
                if (string.IsNullOrWhiteSpace(FarmPriceText))
                    FarmPriceError = ""; // Optional field, no error
                else if (FarmPrice <= 0)
                    FarmPriceError = "يجب أن يكون السعر أكبر من صفر";
                else
                    FarmPriceError = "";
            }

            private void ValidateFarmSpecialData()
            {
                // Optional numeric field; ensure positive if provided
                if (string.IsNullOrWhiteSpace(FarmSpecialDataNumberText))
                {
                    FarmSpecialDataError = "";
                }
                else if (!FarmSpecialDataNumber.HasValue || FarmSpecialDataNumber.Value <= 0)
                {
                    FarmSpecialDataError = "القيمة يجب أن تكون رقمًا أكبر من صفر";
                }
                else
                {
                    FarmSpecialDataError = "";
                }
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
                if (!FactoryDiscount.HasValue)
                    FactoryDiscountError = ""; // Optional field, no error
                else if (FactoryDiscount < 0 || FactoryDiscount > 100)
                    FactoryDiscountError = "النسبة يجب أن تكون بين 0 و 100";
                else
                    FactoryDiscountError = "";
            }

            private void ValidateFactoryPrice()
            {
                if (string.IsNullOrWhiteSpace(FactoryPriceText))
                    FactoryPriceError = ""; // Optional field, no error
                else if (FactoryPrice <= 0)
                    FactoryPriceError = "يجب أن يكون السعر أكبر من صفر";
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

            // Robust decimal parser supporting both comma and dot as decimal separators
            private static bool TryParseDecimalFlexible(string text, out decimal value)
            {
                value = 0m;
                if (string.IsNullOrWhiteSpace(text)) return false;

                var trimmed = text.Trim();
                // Try current culture
                if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.CurrentCulture, out value)) return true;
                // Try invariant
                if (decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return true;
                // Replace comma with dot and try invariant
                var normalized = trimmed.Replace(',', '.');
                return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
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



        // Event handler for numeric-only input validation
        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Get current text and the position where new text will be inserted
            string currentText = textBox.Text;
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            // Simulate what the text would be after the input
            string newText = currentText.Remove(selectionStart, selectionLength).Insert(selectionStart, e.Text);

            // Allow only digits and one decimal separator (. or ,)
            bool isValidInput = true;
            bool hasDecimalSeparator = false;

            foreach (char c in newText)
            {
                if (char.IsDigit(c))
                {
                    continue;
                }
                else if ((c == '.' || c == ',') && !hasDecimalSeparator)
                {
                    hasDecimalSeparator = true;
                }
                else
                {
                    isValidInput = false;
                    break;
                }
            }

            // Also validate that it can be parsed as decimal
            if (isValidInput && !string.IsNullOrEmpty(newText))
            {
                // Replace comma with dot for parsing
                string testText = newText.Replace(',', '.');
                isValidInput = decimal.TryParse(testText, out _);
            }

            e.Handled = !isValidInput;
        }

        // Prevent paste operations that contain non-numeric characters
        private void CommandManager_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    string clipboardText = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        // Check if clipboard contains only numeric characters
                        bool isNumeric = decimal.TryParse(clipboardText, out _);
                        if (!isNumeric)
                        {
                            e.Handled = true; // Cancel paste operation
                        }
                    }
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
                Farm? selectedFarm = null;
                if (ViewModel.SelectedFarm != null && !string.IsNullOrWhiteSpace(ViewModel.SelectedFarm.FarmName))
                {
                    selectedFarm = ViewModel.SelectedFarm;
                    if (selectedFarm.FarmId == 0) // New farm
                    {
                        context.Farms.Add(selectedFarm);
                        context.SaveChanges();
                    }
                }

                // Handle OPTIONAL Factory (only if user entered data)
                Factory? selectedFactory = null;
                if (ViewModel.SelectedFactory != null && !string.IsNullOrWhiteSpace(ViewModel.SelectedFactory.FactoryName))
                {
                    selectedFactory = ViewModel.SelectedFactory;
                    if (selectedFactory.FactoryId == 0) // New factory
                    {
                        context.Factories.Add(selectedFactory);
                        context.SaveChanges();
                    }
                }

                // REQUIRED Truck: find or create
                var selectedTruck = context.Trucks.FirstOrDefault(t => t.TruckNumber == ViewModel.TruckNumber);
                if (selectedTruck == null)
                {
                    selectedTruck = new Truck { TruckNumber = ViewModel.TruckNumber };
                    context.Trucks.Add(selectedTruck);
                    context.SaveChanges();
                }

                // Save the entry
                var supplyEntry = new SupplyEntry
                {
                    EntryDate = ViewModel.Date ?? DateTime.Today,
                    TruckId = selectedTruck.TruckId,
                    FarmId = selectedFarm?.FarmId,
                    FarmWeight = ViewModel.FarmWeight,
                    FarmDiscountRate = ViewModel.FarmDiscountPercentage,
                    FarmPricePerKilo = ViewModel.FarmPrice,
                    FactoryId = selectedFactory?.FactoryId,
                    FactoryWeight = ViewModel.FactoryWeight,
                    FactoryDiscountRate = ViewModel.FactoryDiscount,
                    FactoryPricePerKilo = ViewModel.FactoryPrice,
                    FreightCost = ViewModel.TransportPrice,
                    Notes = ViewModel.Notes
                };

                context.SupplyEntries.Add(supplyEntry);
                context.SaveChanges();

                // Auto-create a cash disbursement for the Farm ONLY if special data number is valid
                if (selectedFarm != null)
                {
                    decimal farmTotalAmount = 0m;
                    if (ViewModel.FarmTotal.HasValue)
                    {
                        farmTotalAmount = ViewModel.FarmTotal.Value;
                    }
                    else if (ViewModel.FarmAllowedWeight.HasValue && ViewModel.FarmPrice.HasValue)
                    {
                        farmTotalAmount = ViewModel.FarmAllowedWeight.Value * ViewModel.FarmPrice.Value;
                    }

                    // Only create cash disbursement if cash disbursement amount is provided
                    if (ViewModel.FarmSpecialDataNumber.HasValue && ViewModel.FarmSpecialDataNumber.Value > 0)
                    {
                        // Create cash disbursement with the specified amount
                        var disbFarm = new CashDisbursementTransaction
                        {
                            EntityName = selectedFarm.FarmName,
                            TransactionDate = ViewModel.Date ?? DateTime.Today,
                            Amount = ViewModel.FarmSpecialDataNumber.Value
                        };
                        context.CashDisbursementTransactions.Add(disbFarm);
                        context.SaveChanges();
                    }
                }

                // Keep: optional auto disbursement for transport contractor (if provided)
                if (!string.IsNullOrWhiteSpace(ViewModel.TransportContractorName) && ViewModel.TransportPrice.HasValue && ViewModel.TransportPrice.Value > 0)
                {
                    var cons = new Contractor
                    {
                        ContractorName = ViewModel.TransportContractorName.Trim(),
                        RelatedFramName = ViewModel.SelectedFarm?.FarmName ?? string.Empty,
                        RelatedFactoryName = ViewModel.SelectedFactory?.FactoryName ?? string.Empty 

                    };
                    context.Contractors.Add(cons);
                    context.SaveChanges();
                }
                
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
            ViewModel.TransportPriceText = string.Empty;
            ViewModel.SelectedFarm = null;
            ViewModel.FarmId = null;
            ViewModel.FarmWeight = null;
            ViewModel.FarmDiscountPercentage = null;
            ViewModel.FarmAllowedWeight = null;
            ViewModel.FarmPrice = null;
            ViewModel.FarmPriceText = string.Empty;
            ViewModel.FarmTotal = null;
            ViewModel.SelectedFactory = null;
            ViewModel.FactoryId = null;
            ViewModel.FactoryWeight = null;
            ViewModel.FactoryDiscount = null;
            ViewModel.FactoryAllowedWeight = null;
            ViewModel.FactoryPrice = null;
            ViewModel.FactoryPriceText = string.Empty;
            ViewModel.FactoryTotal = null;
            ViewModel.ProfitMargin = null;
            ViewModel.Notes = string.Empty;
            ViewModel.FarmSpecialDataNumber = null;

            ViewModel.TruckNumberError = string.Empty;
            ViewModel.TransportPriceError = string.Empty;
            ViewModel.FarmNameError = string.Empty;
            ViewModel.FarmWeightError = string.Empty;
            ViewModel.FarmDiscountError = string.Empty;
            ViewModel.FarmPriceError = string.Empty;
            ViewModel.FarmSpecialDataError = string.Empty;
            ViewModel.FactoryNameError = string.Empty;
            ViewModel.FactoryWeightError = string.Empty;
            ViewModel.FactoryDiscountError = string.Empty;
            ViewModel.FactoryPriceError = string.Empty;
        }

        private void ClearFarmSection()
        {
            ViewModel.SelectedFarm = null;
            ViewModel.FarmWeight = null;
            ViewModel.FarmDiscountPercentage = null;
            ViewModel.FarmPrice = null;
        }

        private void ClearFactorySection()
        {
            ViewModel.SelectedFactory = null;
            ViewModel.FactoryWeight = null;
            ViewModel.FactoryDiscount = null;
            ViewModel.FactoryPrice = null;
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

