using System.Windows;
using System.Windows.Input;
using Fruittrack.Services;

namespace Fruittrack.Views
{
    /// <summary>
    /// Interaction logic for PasswordDialog.xaml
    /// </summary>
    public partial class PasswordDialog : Window
    {
        private readonly LicensingService _licensingService;
        public bool IsAuthenticated { get; private set; }

        public PasswordDialog()
        {
            InitializeComponent();
            _licensingService = new LicensingService();
            LoadDeviceInfo();
        }

        private void LoadDeviceInfo()
        {
            try
            {
                string currentMac = _licensingService.GetCurrentMacAddress();
                string storedMac = _licensingService.GetStoredMacAddress();

                if (!string.IsNullOrEmpty(currentMac))
                {
                    DeviceInfoText.Text = $"MAC Address: {currentMac}";
                    
                    if (!string.IsNullOrEmpty(storedMac))
                    {
                        DeviceInfoText.Text += $"\nStored MAC: {storedMac}";
                        
                        if (currentMac.Equals(storedMac, System.StringComparison.OrdinalIgnoreCase))
                        {
                            DeviceInfoText.Text += "\n✅ Device matches stored license";
                        }
                        else
                        {
                            DeviceInfoText.Text += "\n❌ Device doesn't match stored license";
                        }
                    }
                    else
                    {
                        DeviceInfoText.Text += "\n❌ No stored license found";
                    }
                }
                else
                {
                    DeviceInfoText.Text = "❌ Could not retrieve device information";
                }
            }
            catch
            {
                DeviceInfoText.Text = "❌ Error loading device information";
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Authenticate();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsAuthenticated = false;
            Close();
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Authenticate();
            }
        }

        private void Authenticate()
        {
            string password = PasswordBox.Password;
            
            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("الرجاء إدخال كلمة المرور", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_licensingService.AuthenticateWithPassword(password))
                {
                    IsAuthenticated = true;
                    MessageBox.Show("تم التأكيد بنجاح! سيتم حفظ معلومات الجهاز.", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                    Close();
                }
                else
                {
                    MessageBox.Show("كلمة المرور غير صحيحة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    PasswordBox.Password = "";
                    PasswordBox.Focus();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"خطأ في المصادقة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 