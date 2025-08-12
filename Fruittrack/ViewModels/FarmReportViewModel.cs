using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Fruittrack.Models;
using Microsoft.EntityFrameworkCore;

namespace Fruittrack.ViewModels
{
    public class FarmReportRow
    {
        public DateTime Date { get; set; }
        public string TruckNumber { get; set; } = string.Empty;
        public string FarmName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal AllowedWeight { get; set; }
        public decimal PricePerKilo { get; set; }
        public decimal FarmTotal { get; set; }
    }

    public class FarmReportViewModel : INotifyPropertyChanged
    {
        private readonly FruitTrackDbContext _dbContext;

        public FarmReportViewModel(FruitTrackDbContext dbContext)
        {
            _dbContext = dbContext;
            FromDate = DateTime.Today.AddDays(-7);
            ToDate = DateTime.Today;
            Rows = new ObservableCollection<FarmReportRow>();
            RefreshCommand = new RelayCommand(_ => LoadData());
            LoadData();
        }

        private DateTime _fromDate;
        public DateTime FromDate
        {
            get => _fromDate;
            set { _fromDate = value; OnPropertyChanged(nameof(FromDate)); }
        }

        private DateTime _toDate;
        public DateTime ToDate
        {
            get => _toDate;
            set { _toDate = value; OnPropertyChanged(nameof(ToDate)); }
        }

        public ObservableCollection<FarmReportRow> Rows { get; }

        private decimal _totalWeight;
        public decimal TotalWeight { get => _totalWeight; private set { _totalWeight = value; OnPropertyChanged(nameof(TotalWeight)); OnPropertyChanged(nameof(FormattedTotalWeight)); } }
        public string FormattedTotalWeight => TotalWeight.ToString("N2", new CultureInfo("ar-EG"));

        private decimal _totalAllowedWeight;
        public decimal TotalAllowedWeight { get => _totalAllowedWeight; private set { _totalAllowedWeight = value; OnPropertyChanged(nameof(TotalAllowedWeight)); OnPropertyChanged(nameof(FormattedTotalAllowedWeight)); } }
        public string FormattedTotalAllowedWeight => TotalAllowedWeight.ToString("N2", new CultureInfo("ar-EG"));

        private decimal _totalFarmAmount;
        public decimal TotalFarmAmount { get => _totalFarmAmount; private set { _totalFarmAmount = value; OnPropertyChanged(nameof(TotalFarmAmount)); OnPropertyChanged(nameof(FormattedTotalFarmAmount)); OnPropertyChanged(nameof(NetAmount)); OnPropertyChanged(nameof(FormattedNetAmount)); } }
        public string FormattedTotalFarmAmount => TotalFarmAmount.ToString("N2", new CultureInfo("ar-EG"));

        private decimal _totalDisbursed;
        public decimal TotalDisbursed { get => _totalDisbursed; private set { _totalDisbursed = value; OnPropertyChanged(nameof(TotalDisbursed)); OnPropertyChanged(nameof(FormattedTotalDisbursed)); OnPropertyChanged(nameof(NetAmount)); OnPropertyChanged(nameof(FormattedNetAmount)); } }
        public string FormattedTotalDisbursed => TotalDisbursed.ToString("N2", new CultureInfo("ar-EG"));

        public decimal NetAmount => TotalFarmAmount - TotalDisbursed;
        public string FormattedNetAmount => NetAmount.ToString("N2", new CultureInfo("ar-EG"));

        public ICommand RefreshCommand { get; }

        public void LoadData()
        {
            Rows.Clear();

            var from = FromDate.Date;
            var to = ToDate.Date.AddDays(1).AddTicks(-1);

            var entries = _dbContext.SupplyEntries
                .Include(e => e.Truck)
                .Include(e => e.Farm)
                .Where(e => e.EntryDate >= from && e.EntryDate <= to && e.FarmId != null)
                .OrderByDescending(e => e.EntryDate)
                .ToList();

            decimal totalWeight = 0m;
            decimal totalAllowed = 0m;
            decimal totalAmount = 0m;

            foreach (var e in entries)
            {
                var weight = e.FarmWeight ?? 0m;
                var discount = e.FarmDiscountRate ?? 0m;
                var price = e.FarmPricePerKilo ?? 0m;
                var allowed = weight * (1 - (discount / 100m));
                var amount = allowed * price;

                Rows.Add(new FarmReportRow
                {
                    Date = e.EntryDate,
                    TruckNumber = e.Truck?.TruckNumber ?? string.Empty,
                    FarmName = e.Farm?.FarmName ?? string.Empty,
                    Weight = weight,
                    DiscountPercent = discount,
                    AllowedWeight = allowed,
                    PricePerKilo = price,
                    FarmTotal = amount
                });

                totalWeight += weight;
                totalAllowed += allowed;
                totalAmount += amount;
            }

            TotalWeight = totalWeight;
            TotalAllowedWeight = totalAllowed;
            TotalFarmAmount = totalAmount;

            // Sum all cash disbursements in range (as specified)
            TotalDisbursed = _dbContext.CashDisbursementTransactions
                .Where(d => d.TransactionDate >= from && d.TransactionDate <= to)
                .Select(d => (decimal?)d.Amount)
                .Sum() ?? 0m;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}



