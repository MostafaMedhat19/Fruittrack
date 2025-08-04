# Hardware-Based Licensing System - Fruittrack

## Overview

This document describes the implementation of a hardware-based licensing mechanism for the Fruittrack WPF application. The system uses MAC address verification and password authentication to ensure the application is only used on authorized devices.

## 🔧 Features

### **Security Features**
- ✅ **Password Protection**: SHA256 hashed password verification
- ✅ **Hardware Binding**: MAC address-based device identification
- ✅ **Persistent Storage**: License information stored in `auth.lock` file
- ✅ **Automatic Verification**: License check on application startup
- ✅ **Device Migration**: Support for moving to new devices with password re-entry

### **User Experience**
- ✅ **First Launch**: Password prompt for new installations
- ✅ **Subsequent Launches**: Automatic verification without password
- ✅ **Device Change**: Password prompt when moved to different hardware
- ✅ **License Management**: Built-in license information and management tools

## 🏗️ Architecture

### **Core Components**

#### 1. **LicensingService** (`Services/LicensingService.cs`)
- Handles MAC address retrieval and comparison
- Manages password hashing and verification
- Controls `auth.lock` file operations
- Provides device information utilities

#### 2. **LicenseManager** (`Services/LicenseManager.cs`)
- Orchestrates the licensing flow
- Manages password dialog display
- Handles license status checks
- Provides license management functions

#### 3. **PasswordDialog** (`Views/PasswordDialog.xaml`)
- Modern authentication interface
- Device information display
- Password input with validation
- RTL layout for Arabic support

#### 4. **LicenseManagementPage** (`LicenseManagementPage.xaml`)
- License information display
- License management tools
- Device status monitoring

## 🔐 Security Implementation

### **Password Hashing**
```csharp
// The password "Hasan@2025Mostafa!" is stored as SHA256 hash
private const string HARDCODED_PASSWORD_HASH = "8C6976E5B5410415BDE908BD4DEE15DFB167A9C873FC4BB8A81F6F2AB448A918";
```

### **MAC Address Retrieval**
```csharp
public string GetCurrentMacAddress()
{
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
```

### **License File Structure**
The `auth.lock` file contains:
- MAC address of the authorized device
- Created timestamp (for license date tracking)
- Simple text format for easy debugging

## 📋 Usage Instructions

### **For End Users**

#### **First Installation**
1. Launch the application
2. Enter the password: `Hasan@2025Mostafa!`
3. Click "تأكيد" (Confirm)
4. The application will save the device's MAC address
5. Future launches will be automatic

#### **Moving to New Device**
1. Copy the application to the new device
2. Launch the application
3. Enter the password again
4. The new device's MAC address will be saved

#### **License Management**
- Access the License Management page to view device information
- Use "حذف الترخيص" to clear the license (requires password re-entry)
- Use "تحديث المعلومات" to refresh license status

### **For Developers**

#### **Integration with Main Application**
```csharp
// In App.xaml.cs - OnStartup method
var licenseManager = new LicenseManager();
if (!licenseManager.CheckLicense())
{
    Shutdown();
    return;
}
```

#### **Adding License Management to Menu**
```csharp
// Add to your main window navigation
private void LicenseManagement_Click(object sender, RoutedEventArgs e)
{
    var licensePage = new LicenseManagementPage();
    NavigationService.Navigate(licensePage);
}
```

#### **Generating New Password Hash**
```csharp
// Use the PasswordHashGenerator utility
string newHash = PasswordHashGenerator.GenerateHash("YourNewPassword");
Console.WriteLine($"SHA256 Hash: {newHash}");
```

## 🔄 License Flow

### **Application Startup**
```
1. Application starts
2. Check if auth.lock exists
3. If exists:
   - Read stored MAC address
   - Get current MAC address
   - Compare addresses
   - If match → Continue to application
   - If no match → Show password dialog
4. If not exists:
   - Show password dialog
5. If password correct:
   - Save current MAC address to auth.lock
   - Continue to application
6. If password incorrect:
   - Show error message
   - Exit application
```

### **Password Dialog Flow**
```
1. Display device information
2. Show password input field
3. User enters password
4. Hash password with SHA256
5. Compare with hardcoded hash
6. If correct:
   - Save MAC address
   - Close dialog
   - Continue to application
7. If incorrect:
   - Show error message
   - Clear password field
   - Allow retry
```

## 🛠️ Configuration

### **Changing the Password**
1. Generate a new hash using `PasswordHashGenerator`
2. Update the `HARDCODED_PASSWORD_HASH` constant in `LicensingService.cs`
3. Rebuild the application

### **Customizing the License File**
- Change `AUTH_FILE` constant in `LicensingService.cs`
- Modify file format in `AuthenticateWithPassword` method
- Update reading logic in `IsLicensed` method

### **Adding Additional Hardware Identifiers**
```csharp
// Example: Add CPU ID or Motherboard Serial
public string GetHardwareId()
{
    // Combine multiple identifiers
    string macAddress = GetCurrentMacAddress();
    string cpuId = GetCpuId();
    return $"{macAddress}|{cpuId}";
}
```

## 🔍 Troubleshooting

### **Common Issues**

#### **"Could not retrieve device information"**
- Check network adapter status
- Ensure application has network access permissions
- Verify Windows network services are running

#### **"License file not found"**
- Normal for first installation
- Check file permissions in application directory
- Verify `auth.lock` file exists and is readable

#### **"Device doesn't match stored license"**
- Normal when moving to new device
- Enter password to authorize new device
- Check if MAC address has changed on same device

#### **"Password incorrect"**
- Verify password: `Hasan@2025Mostafa!`
- Check for extra spaces or special characters
- Ensure correct keyboard layout

### **Debug Information**
```csharp
// Enable debug logging
var licenseManager = new LicenseManager();
string deviceInfo = licenseManager.GetCurrentDeviceInfo();
Console.WriteLine(deviceInfo);
```

## 🔒 Security Considerations

### **Strengths**
- ✅ Hardware binding prevents simple copying
- ✅ Password hashing prevents reverse engineering
- ✅ Automatic verification on startup
- ✅ Support for device migration

### **Limitations**
- MAC addresses can be spoofed (advanced users)
- `auth.lock` file can be deleted (resets license)
- Single password for all installations

### **Recommendations**
- Consider obfuscating the password hash
- Add additional hardware identifiers
- Implement online license verification
- Add license expiration dates
- Use stronger encryption for license file

## 📁 File Structure

```
Fruittrack/
├── Services/
│   ├── LicensingService.cs          # Core licensing logic
│   └── LicenseManager.cs            # License flow management
├── Views/
│   └── PasswordDialog.xaml          # Authentication UI
├── Utilities/
│   └── PasswordHashGenerator.cs     # Hash generation utility
├── LicenseManagementPage.xaml       # License management UI
└── auth.lock                        # License file (created at runtime)
```

## 🚀 Deployment

### **Build Requirements**
- .NET Framework 4.7.2 or higher
- WPF support
- Network access permissions

### **Installation**
1. Copy application files to target device
2. Ensure network adapter is enabled
3. Run application as administrator (first time)
4. Enter password when prompted
5. Application is now licensed for this device

### **Distribution**
- Include all licensing components in deployment
- Ensure `auth.lock` file is not included in distribution
- Test on target hardware before distribution

## 📞 Support

For issues with the licensing system:
1. Check device network configuration
2. Verify password is correct
3. Clear license and re-authenticate
4. Check application permissions
5. Review debug information in console

---

**Note**: This licensing system provides basic protection against casual copying. For enterprise applications, consider implementing additional security measures such as online verification, hardware dongles, or more sophisticated encryption. 