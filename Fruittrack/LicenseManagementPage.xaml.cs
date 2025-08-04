using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Fruittrack.Services;

namespace Fruittrack
{
    /// <summary>
    /// Interaction logic for LicenseManagementPage.xaml
    /// </summary>
    public partial class LicenseManagementPage : Page
    {
        private readonly LicenseManager _licenseManager;
        private readonly LicensingService _licensingService;

        public LicenseManagementPage()
        {
            InitializeComponent();
            _licenseManager = new LicenseManager();
            _licensingService = new LicensingService();
            LoadLicenseInfo();
        }

        private void LoadLicenseInfo()
        {
            try
            {
                // Get current device info
                string currentMac = _licensingService.GetCurrentMacAddress();
                CurrentDeviceText.Text = currentMac ?? "غير متوفر";

                // Get stored device info
                string storedMac = _licensingService.GetStoredMacAddress();
                StoredDeviceText.Text = storedMac ?? "غير موجود";

                // Get license status
                bool isLicensed = _licensingService.IsLicensed();
                LicenseStatusText.Text = isLicensed ? "مرخص ✅" : "غير مرخص ❌";
                LicenseStatusText.Foreground = isLicensed ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;

                // Get license date
                string licenseDate = GetLicenseDate();
                LicenseDateText.Text = licenseDate ?? "غير متوفر";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل معلومات الترخيص: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetLicenseDate()
        {
            try
            {
                if (File.Exists("auth.lock"))
                {
                    var fileInfo = new FileInfo("auth.lock");
                    return fileInfo.CreationTime.ToString("dd/MM/yyyy HH:mm");
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void RefreshInfo_Click(object sender, RoutedEventArgs e)
        {
            LoadLicenseInfo();
            MessageBox.Show("تم تحديث المعلومات", "تم التحديث", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearLicense_Click(object sender, RoutedEventArgs e)
        {
            _licenseManager.ClearLicense();
            LoadLicenseInfo();
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow;
            if (mainWindow is NavigationWindow navigationWindow)
            {
                navigationWindow.GoBack();
            }
        }
    }
} 