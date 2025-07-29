using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Navigation;
using Fruittrack.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System;

namespace Fruittrack
{
    public partial class MainWindow : NavigationWindow
    {

        public MainWindow()
        {
            InitializeComponent();


            // Test EF Core connection and add sample data
            try
            {
                using (var context = App.ServiceProvider.GetRequiredService<FruitTrackDbContext>())
                {
                    context.Database.EnsureCreated(); // Ensure DB exists

                    // Add sample data if not exists
                    if (!context.Trucks.Any())
                    {
                        var truck = new Truck { TruckNumber = "TRK-001" };
                        var farm = new Farm { FarmName = "Green Farm" };
                        var factory = new Factory { FactoryName = "Juice Factory" };
                        context.Trucks.Add(truck);
                        context.Farms.Add(farm);
                        context.Factories.Add(factory);
                        context.SaveChanges();

                        var supplyEntry = new SupplyEntry
                        {
                            EntryDate = DateTime.Now,
                            TruckId = truck.TruckId,
                            FarmId = farm.FarmId,
                            FarmWeight = 10.5m,
                            FarmDiscountRate = 0.05m,
                            FarmPricePerKilo = 0.2m, // 0.2 SAR per kilo (was 200 per ton)
                            FactoryId = factory.FactoryId,
                            FactoryWeight = 10.0m,
                            FactoryDiscountRate = 0.03m,
                            FactoryPricePerKilo = 0.22m, // 0.22 SAR per kilo (was 220 per ton)
                            FreightCost = 50m
                        };
                        context.SupplyEntries.Add(supplyEntry);
                        context.SaveChanges();

                        var settlement = new FinancialSettlement
                        {
                            SupplyEntryId = supplyEntry.SupplyEntryId,
                            ExpectedAmount = 2100m,
                            ReceivedAmount = 2100m
                        };
                        context.FinancialSettlements.Add(settlement);
                        context.SaveChanges();
                    }
                }
              
            }
            catch (Exception ex)
            {
                MessageBox.Show($"EF Core test failed: {ex.Message}", "EF Core Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
