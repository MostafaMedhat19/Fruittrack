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
    public class FactoryReportRow
    {
        public DateTime Date { get; set; }
        public string TruckNumber { get; set; } = string.Empty;
        public string FactoryName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal AllowedWeight { get; set; }
        public decimal PricePerKilo { get; set; }
        public decimal FactoryTotal { get; set; }
        public string TransportContractorName { get; set; } = string.Empty;
    }

    public class FactoryReportViewModel : INotifyPropertyChanged
    {
        private readonly FruitTrackDbContext _dbContext;

        public FactoryReportViewModel(FruitTrackDbContext dbContext)
        {
            _dbContext = dbContext;
            FromDate = DateTime.Today.AddDays(-7);
            ToDate = DateTime.Today;
            Rows = new ObservableCollection<FactoryReportRow>();
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

        public ObservableCollection<FactoryReportRow> Rows { get; }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    ApplySearchFilter();
                }
            }
        }

        private decimal _totalWeight;
        public decimal TotalWeight { get => _totalWeight; private set { _totalWeight = value; OnPropertyChanged(nameof(TotalWeight)); OnPropertyChanged(nameof(FormattedTotalWeight)); } }
        public string FormattedTotalWeight => TotalWeight.ToString("N2", new CultureInfo("ar-EG"));

        private decimal _totalAllowedWeight;
        public decimal TotalAllowedWeight { get => _totalAllowedWeight; private set { _totalAllowedWeight = value; OnPropertyChanged(nameof(TotalAllowedWeight)); OnPropertyChanged(nameof(FormattedTotalAllowedWeight)); } }
        public string FormattedTotalAllowedWeight => TotalAllowedWeight.ToString("N2", new CultureInfo("ar-EG"));

        private decimal _totalFactoryAmount;
        public decimal TotalFactoryAmount { get => _totalFactoryAmount; private set { _totalFactoryAmount = value; OnPropertyChanged(nameof(TotalFactoryAmount)); OnPropertyChanged(nameof(FormattedTotalFactoryAmount)); OnPropertyChanged(nameof(NetAmount)); OnPropertyChanged(nameof(FormattedNetAmount)); } }
        public string FormattedTotalFactoryAmount => TotalFactoryAmount.ToString("N2", new CultureInfo("ar-EG"));

        private decimal _totalReceived;
        public decimal TotalReceived { get => _totalReceived; private set { _totalReceived = value; OnPropertyChanged(nameof(TotalReceived)); OnPropertyChanged(nameof(FormattedTotalReceived)); OnPropertyChanged(nameof(NetAmount)); OnPropertyChanged(nameof(FormattedNetAmount)); } }
        public string FormattedTotalReceived => TotalReceived.ToString("N2", new CultureInfo("ar-EG"));

        public decimal NetAmount => TotalFactoryAmount - TotalReceived;
        public string FormattedNetAmount => NetAmount.ToString("N2", new CultureInfo("ar-EG"));

        public ICommand RefreshCommand { get; }

        public void LoadData()
        {
            Rows.Clear();

            var from = FromDate.Date;
            var to = ToDate.Date.AddDays(1).AddTicks(-1);

            var entries = _dbContext.SupplyEntries
                .Include(e => e.Truck)
                .Include(e => e.Factory)
                .Where(e => e.EntryDate >= from && e.EntryDate <= to && e.FactoryId != null)
                .OrderByDescending(e => e.EntryDate)
                .ToList();

            decimal totalWeight = 0m;
            decimal totalAllowed = 0m;
            decimal totalAmount = 0m;

            foreach (var e in entries)
            {
                var weight = e.FactoryWeight ?? 0m;
                var discount = e.FactoryDiscountRate ?? 0m;
                var price = e.FactoryPricePerKilo ?? 0m;
                var allowed = weight * (1 - (discount / 100m));
                var amount = allowed * price;

                // Get contractor name from Contractor table based on factory name relationship
                string contractorName = string.Empty;
                if (!string.IsNullOrEmpty(e.Factory?.FactoryName))
                {
                    var contractor = _dbContext.Contractors
                        .Where(c => c.RelatedFactoryName == e.Factory.FactoryName)
                        .Select(c => c.ContractorName)
                        .FirstOrDefault();
                    contractorName = contractor ?? string.Empty;
                }

                Rows.Add(new FactoryReportRow
                {
                    Date = e.EntryDate,
                    TruckNumber = e.Truck?.TruckNumber ?? string.Empty,
                    FactoryName = e.Factory?.FactoryName ?? string.Empty,
                    Weight = weight,
                    DiscountPercent = discount,
                    AllowedWeight = allowed,
                    PricePerKilo = price,
                    FactoryTotal = amount,
                    TransportContractorName = contractorName
                });

                totalWeight += weight;
                totalAllowed += allowed;
                totalAmount += amount;
            }

            TotalWeight = totalWeight;
            TotalAllowedWeight = totalAllowed;
            TotalFactoryAmount = totalAmount;

            // Sum all cash receipts in range (as specified)
            TotalReceived = _dbContext.CashReceiptTransactions
                .Where(r => r.Date >= from && r.Date <= to)
                .Select(r => (decimal?)r.ReceivedAmount)
                .Sum() ?? 0m;

            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            if (Rows == null)
                return;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var filtered = Rows.Where(r => !string.IsNullOrEmpty(r.FactoryName) && r.FactoryName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
                if (filtered.Count != Rows.Count)
                {
                    Rows.Clear();
                    foreach (var item in filtered)
                        Rows.Add(item);
                }

                TotalWeight = Rows.Sum(r => r.Weight);
                TotalAllowedWeight = Rows.Sum(r => r.AllowedWeight);
                TotalFactoryAmount = Rows.Sum(r => r.FactoryTotal);
            }
            else
            {
                // Reload current date range to restore list
                var from = FromDate.Date;
                var to = ToDate.Date.AddDays(1).AddTicks(-1);
                var entries = _dbContext.SupplyEntries
                    .Include(e => e.Truck)
                    .Include(e => e.Factory)
                    .Where(e => e.EntryDate >= from && e.EntryDate <= to && e.FactoryId != null)
                    .OrderByDescending(e => e.EntryDate)
                    .ToList();

                Rows.Clear();
                foreach (var e in entries)
                {
                    var weight = e.FactoryWeight ?? 0m;
                    var discount = e.FactoryDiscountRate ?? 0m;
                    var price = e.FactoryPricePerKilo ?? 0m;
                    var allowed = weight * (1 - (discount / 100m));
                    var amount = allowed * price;

                    // Get contractor name from Contractor table based on factory name relationship
                    string contractorName = string.Empty;
                    if (!string.IsNullOrEmpty(e.Factory?.FactoryName))
                    {
                        var contractor = _dbContext.Contractors
                            .Where(c => c.RelatedFactoryName == e.Factory.FactoryName)
                            .Select(c => c.ContractorName)
                            .FirstOrDefault();
                        contractorName = contractor ?? string.Empty;
                    }

                    Rows.Add(new FactoryReportRow
                    {
                        Date = e.EntryDate,
                        TruckNumber = e.Truck?.TruckNumber ?? string.Empty,
                        FactoryName = e.Factory?.FactoryName ?? string.Empty,
                        Weight = weight,
                        DiscountPercent = discount,
                        AllowedWeight = allowed,
                        PricePerKilo = price,
                        FactoryTotal = amount,
                        TransportContractorName = contractorName
                    });
                }

                TotalWeight = Rows.Sum(r => r.Weight);
                TotalAllowedWeight = Rows.Sum(r => r.AllowedWeight);
                TotalFactoryAmount = Rows.Sum(r => r.FactoryTotal);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}



