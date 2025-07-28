using Fruittrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fruittrack
{
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
            private DateTime? _date = DateTime.Now;

            public string TruckNumber { get => _truckNumber; set { _truckNumber = value; OnPropertyChanged(nameof(TruckNumber)); } }
            private string _truckNumber;

            public decimal? TransportPrice { get => _transportPrice; set { _transportPrice = value; OnPropertyChanged(nameof(TransportPrice)); UpdateProfit(); } }
            private decimal? _transportPrice;

            public int? FarmId { get => _farmId; set { _farmId = value; OnPropertyChanged(nameof(FarmId)); } }
            private int? _farmId;

            public decimal? FarmWeight { get => _farmWeight; set { _farmWeight = value; OnPropertyChanged(nameof(FarmWeight)); UpdateFarmAllowedWeight(); } }
            private decimal? _farmWeight;

            public decimal? FarmDiscountPercentage { get => _farmDiscountPercentage; set { _farmDiscountPercentage = value; OnPropertyChanged(nameof(FarmDiscountPercentage)); UpdateFarmAllowedWeight(); } }
            private decimal? _farmDiscountPercentage;

            public decimal? FarmAllowedWeight { get => _farmAllowedWeight; set { _farmAllowedWeight = value; OnPropertyChanged(nameof(FarmAllowedWeight)); UpdateFarmTotal(); } }
            private decimal? _farmAllowedWeight;

            public decimal? FarmPricePerTon { get => _farmPricePerTon; set { _farmPricePerTon = value; OnPropertyChanged(nameof(FarmPricePerTon)); UpdateFarmTotal(); } }
            private decimal? _farmPricePerTon;

            public decimal? FarmTotal { get => _farmTotal; set { _farmTotal = value; OnPropertyChanged(nameof(FarmTotal)); UpdateProfit(); } }
            private decimal? _farmTotal;

            public int? FactoryId { get => _factoryId; set { _factoryId = value; OnPropertyChanged(nameof(FactoryId)); } }
            private int? _factoryId;

            public decimal? FactoryWeight { get => _factoryWeight; set { _factoryWeight = value; OnPropertyChanged(nameof(FactoryWeight)); UpdateFactoryAllowedWeight(); } }
            private decimal? _factoryWeight;

            public decimal? FactoryDiscountPercentage { get => _factoryDiscountPercentage; set { _factoryDiscountPercentage = value; OnPropertyChanged(nameof(FactoryDiscountPercentage)); UpdateFactoryAllowedWeight(); } }
            private decimal? _factoryDiscountPercentage;

            public decimal? FactoryAllowedWeight { get => _factoryAllowedWeight; set { _factoryAllowedWeight = value; OnPropertyChanged(nameof(FactoryAllowedWeight)); UpdateFactoryTotal(); } }
            private decimal? _factoryAllowedWeight;

            public decimal? FactoryPricePerTon { get => _factoryPricePerTon; set { _factoryPricePerTon = value; OnPropertyChanged(nameof(FactoryPricePerTon)); UpdateFactoryTotal(); } }
            private decimal? _factoryPricePerTon;

            public decimal? FactoryTotal { get => _factoryTotal; set { _factoryTotal = value; OnPropertyChanged(nameof(FactoryTotal)); UpdateProfit(); } }
            private decimal? _factoryTotal;

            public decimal? ProfitMargin { get => _profitMargin; set { _profitMargin = value; OnPropertyChanged(nameof(ProfitMargin)); } }
            private decimal? _profitMargin;

            public string Notes { get => _notes; set { _notes = value; OnPropertyChanged(nameof(Notes)); } }
            private string _notes;

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
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            var errors = ValidateInputs();
            if (!string.IsNullOrEmpty(errors))
            {
                MessageBox.Show(errors, "خطأ في الإدخال", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                var context = _context;
                string farmName = FarmName.Text?.Trim();
                string factoryName = FactoryName.Text?.Trim();

                var farm = context.Farms.FirstOrDefault(f => f.FarmName == farmName);
                if (farm == null)
                {
                    farm = new Farm
                    {
                        FarmName = farmName
                    };
                    context.Farms.Add(farm);
                    context.SaveChanges();
                }

                var factory = context.Factories.FirstOrDefault(f => f.FactoryName == factoryName);
                if (factory == null)
                {
                    factory = new Factory
                    {
                        FactoryName = factoryName
                    };
                    context.Factories.Add(factory);
                    context.SaveChanges();
                }

                // Truck: find or create
                var truck = context.Trucks.FirstOrDefault(t => t.TruckNumber == ViewModel.TruckNumber);
                if (truck == null)
                {
                    truck = new Truck { TruckNumber = ViewModel.TruckNumber };
                    context.Trucks.Add(truck);
                    context.SaveChanges();
                }

                // Create SupplyEntry with proper null checks
                var supplyEntry = new SupplyEntry
                {
                    EntryDate = CalendarControl.SelectedDate ?? DateTime.Now,
                    TruckId = truck.TruckId,
                    FarmId = ViewModel.FarmId ?? throw new InvalidOperationException("FarmId is required"),
                    FarmWeight = ViewModel.FarmWeight ?? throw new InvalidOperationException("FarmWeight is required"),
                    FarmDiscountRate = ViewModel.FarmDiscountPercentage ?? throw new InvalidOperationException("FarmDiscountPercentage is required"),
                    FarmPricePerTon = ViewModel.FarmPricePerTon ?? throw new InvalidOperationException("FarmPricePerTon is required"),
                    FactoryId = ViewModel.FactoryId ?? throw new InvalidOperationException("FactoryId is required"),
                    FactoryWeight = ViewModel.FactoryWeight ?? throw new InvalidOperationException("FactoryWeight is required"),
                    FactoryDiscountRate = ViewModel.FactoryDiscountPercentage ?? throw new InvalidOperationException("FactoryDiscountPercentage is required"),
                    FactoryPricePerTon = ViewModel.FactoryPricePerTon ?? throw new InvalidOperationException("FactoryPricePerTon is required"),
                    FreightCost = ViewModel.TransportPrice ?? throw new InvalidOperationException("TransportPrice is required"),
                    TransferFrom = farm.FarmName,
                    TransferTo = factory.FactoryName,
                };

                context.SupplyEntries.Add(supplyEntry);
                context.SaveChanges();
                MessageBox.Show("تم حفظ بيانات التوريد بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء الحفظ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnClear_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            ViewModel.Date = DateTime.Now;
            ViewModel.TruckNumber = string.Empty;
            ViewModel.TransportPrice = null;
            ViewModel.FarmId = null;
            ViewModel.FarmWeight = null;
            ViewModel.FarmDiscountPercentage = null;
            ViewModel.FarmAllowedWeight = null;
            ViewModel.FarmPricePerTon = null;
            ViewModel.FarmTotal = null;
            ViewModel.FactoryId = null;
            ViewModel.FactoryWeight = null;
            ViewModel.FactoryDiscountPercentage = null;
            ViewModel.FactoryAllowedWeight = null;
            ViewModel.FactoryPricePerTon = null;
            ViewModel.FactoryTotal = null;
            ViewModel.ProfitMargin = null;
            ViewModel.Notes = string.Empty;
        }

        private string ValidateInputs()
        {
            var sb = new System.Text.StringBuilder();
            if (string.IsNullOrWhiteSpace(ViewModel.TruckNumber)) sb.AppendLine("يرجى إدخال رقم العربية.");
            if (!ViewModel.TransportPrice.HasValue) sb.AppendLine("يرجى إدخال سعر النقل.");
            if (string.IsNullOrWhiteSpace(FarmName.Text)) sb.AppendLine("يرجى اختيار اسم المزرعة.");
            if (!ViewModel.FarmId.HasValue) sb.AppendLine("يرجى اختيار المزرعة.");
            if (!ViewModel.FarmWeight.HasValue) sb.AppendLine("يرجى إدخال الوزن عند المزرعة.");
            if (!ViewModel.FarmDiscountPercentage.HasValue) sb.AppendLine("يرجى إدخال نسبة الخصم للمزرعة.");
            if (!ViewModel.FarmPricePerTon.HasValue) sb.AppendLine("يرجى إدخال سعر الطن للمزرعة.");
            if (string.IsNullOrWhiteSpace(FactoryName.Text)) sb.AppendLine("يرجى اختيار اسم المصنع.");
            if (!ViewModel.FactoryId.HasValue) sb.AppendLine("يرجى اختيار المصنع.");
            if (!ViewModel.FactoryWeight.HasValue) sb.AppendLine("يرجى إدخال الوزن عند المصنع.");
            if (!ViewModel.FactoryDiscountPercentage.HasValue) sb.AppendLine("يرجى إدخال نسبة الخصم للمصنع.");
            if (!ViewModel.FactoryPricePerTon.HasValue) sb.AppendLine("يرجى إدخال سعر الطن للمصنع.");
            return sb.ToString();
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
