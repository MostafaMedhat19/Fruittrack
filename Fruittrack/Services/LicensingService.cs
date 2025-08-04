using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace Fruittrack.Services
{
    public class LicensingService
    {
        private const string AUTH_FILE = "auth.lock";
        private const string PASSWORD = "admin@2025"; // for testing purposes, use a secure method in production

        public bool IsLicensed()
        {
            try
            {
                // Check if auth file exists
                if (!File.Exists(AUTH_FILE))
                {
                    return false;
                }

                // Read stored MAC address
                string storedMacAddress = File.ReadAllText(AUTH_FILE).Trim();
                if (string.IsNullOrEmpty(storedMacAddress))
                {
                    return false;
                }

                // Get current MAC address
                string currentMacAddress = GetCurrentMacAddress();
                if (string.IsNullOrEmpty(currentMacAddress))
                {
                    return false;
                }

                // Compare MAC addresses
                return storedMacAddress.Equals(currentMacAddress, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking license: {ex.Message}", "License Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public bool AuthenticateWithPassword(string password)
        {
            try
            {
                // Compare the plain text password directly
                if (!password.Equals(PASSWORD, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Password is correct, save MAC address
                string currentMacAddress = GetCurrentMacAddress();
                if (!string.IsNullOrEmpty(currentMacAddress))
                {
                    File.WriteAllText(AUTH_FILE, currentMacAddress);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during authentication: {ex.Message}", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public string GetCurrentMacAddress()
        {
            try
            {
                // Get the first network interface with a physical address
                var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(nic => nic.OperationalStatus == OperationalStatus.Up &&
                                         nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                         !string.IsNullOrEmpty(nic.GetPhysicalAddress().ToString()));

                if (networkInterface != null)
                {
                    var macAddress = networkInterface.GetPhysicalAddress();
                    return string.Join(":", (from z in macAddress.GetAddressBytes() select z.ToString("X2")).ToArray());
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting MAC address: {ex.Message}", "Hardware ID Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToHexString(hashedBytes);
            }
        }

        public void ClearLicense()
        {
            try
            {
                if (File.Exists(AUTH_FILE))
                {
                    File.Delete(AUTH_FILE);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing license: {ex.Message}", "License Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string GetStoredMacAddress()
        {
            try
            {
                if (File.Exists(AUTH_FILE))
                {
                    return File.ReadAllText(AUTH_FILE).Trim();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}