using System;
using System.Windows;
using Fruittrack.Views;

namespace Fruittrack.Services
{
    public class LicenseManager
    {
        private readonly LicensingService _licensingService;

        public LicenseManager()
        {
            _licensingService = new LicensingService();
        }

        public bool CheckLicense()
        {
            try
            {
                // Check if already licensed
                if (_licensingService.IsLicensed())
                {
                    return true;
                }

                // Show password dialog
                var passwordDialog = new PasswordDialog();
                passwordDialog.ShowDialog();

                if (passwordDialog.IsAuthenticated)
                {
                    return true;
                }

                // User cancelled or authentication failed
                MessageBox.Show("يجب إدخال كلمة المرور الصحيحة لاستخدام التطبيق.", 
                              "ترخيص مطلوب", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فحص الترخيص: {ex.Message}", 
                              "خطأ في الترخيص", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                return false;
            }
        }

        public void ShowLicenseInfo()
        {
            try
            {
                string currentMac = _licensingService.GetCurrentMacAddress();
                string storedMac = _licensingService.GetStoredMacAddress();
                bool isLicensed = _licensingService.IsLicensed();

                string info = $"معلومات الترخيص:\n\n";
                info += $"الجهاز الحالي: {currentMac ?? "غير متوفر"}\n";
                info += $"الجهاز المخزن: {storedMac ?? "غير موجود"}\n";
                info += $"الحالة: {(isLicensed ? "مرخص ✅" : "غير مرخص ❌")}";

                MessageBox.Show(info, "معلومات الترخيص", MessageBoxButton.OK, 
                              isLicensed ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في عرض معلومات الترخيص: {ex.Message}", 
                              "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ClearLicense()
        {
            try
            {
                var result = MessageBox.Show("هل أنت متأكد من حذف الترخيص؟ سيتم طلب كلمة المرور مرة أخرى.", 
                                           "تأكيد حذف الترخيص", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _licensingService.ClearLicense();
                    MessageBox.Show("تم حذف الترخيص بنجاح.", "تم الحذف", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حذف الترخيص: {ex.Message}", 
                              "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string GetCurrentDeviceInfo()
        {
            try
            {
                string currentMac = _licensingService.GetCurrentMacAddress();
                string storedMac = _licensingService.GetStoredMacAddress();
                bool isLicensed = _licensingService.IsLicensed();

                return $"Device: {currentMac ?? "N/A"}\n" +
                       $"Stored: {storedMac ?? "None"}\n" +
                       $"Licensed: {(isLicensed ? "Yes" : "No")}";
            }
            catch
            {
                return "Error retrieving device info";
            }
        }
    }
} 