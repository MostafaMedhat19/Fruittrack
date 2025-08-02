# Export Functionality Documentation

## Overview
This document describes the export functionality that has been added to the FruitTrack application. The application now supports printing, PDF export, and Excel export across multiple pages.

## Features Added

### 1. Export Utilities (`ExportUtilities.cs`)
A comprehensive utility class that provides:
- **Print functionality**: Print any page to the default printer
- **PDF export**: Export pages as PDF files with proper formatting
- **Excel export**: Export DataGrid data to Excel files with formatting
- **File dialog helpers**: Standardized file save dialogs

### 2. NuGet Packages Added
- **iTextSharp (5.5.13.3)**: For PDF generation
- **EPPlus (7.0.5)**: For Excel file creation

### 3. Pages with Export Buttons

The following pages now have export buttons (Print, PDF, Excel):

#### Account Statement Page (`AccountStatementPage.xaml`)
- **Print**: Prints the entire account statement page
- **PDF**: Exports to PDF with title and date
- **Excel**: Exports the transactions DataGrid to Excel

#### Supplies Overview Page (`SuppliesOverview.xaml`)
- **Print**: Prints the supplies overview with filters
- **PDF**: Exports filtered data to PDF
- **Excel**: Exports the supplies DataGrid to Excel

#### Production Reports Page (`ProductionReportsPage.xaml`)
- **Print**: Prints production reports with statistics
- **PDF**: Exports reports with summary cards
- **Excel**: Exports the reports DataGrid to Excel

#### Total Profit Summary Page (`TotalProfitSummaryPage.xaml`)
- **Print**: Prints profit summary with charts
- **PDF**: Exports summary with statistics
- **Excel**: Exports the summary DataGrid to Excel

#### Cash Receipt Page (`CashReceiptPage.xaml`)
- **Print**: Prints the cash receipt form
- **PDF**: Exports form data to PDF
- **Excel**: Exports form data as a table

#### Cash Disbursement Page (`CashDisbursementPage.xaml`)
- **Print**: Prints the cash disbursement form
- **PDF**: Exports form data to PDF
- **Excel**: Exports form data as a table

## Usage

### For Users
1. Navigate to any page with export functionality
2. Look for the three colored buttons in the header:
   - **Orange "طباعة"**: Print the current page
   - **Red "PDF"**: Save as PDF file
   - **Green "Excel"**: Save as Excel file
3. Click the desired export button
4. For PDF and Excel, choose a save location when prompted

### For Developers
To add export functionality to a new page:

1. **Add the buttons to XAML**:
```xml
<StackPanel Grid.Column="1" Orientation="Horizontal" VerticalAlignment="Center">
    <Button Content="طباعة" Click="PrintButton_Click"
            Background="#FF9800" Foreground="White"
            Padding="12,8" FontWeight="SemiBold" BorderThickness="0"
            MinWidth="100" Margin="5,0" Cursor="Hand"/>
    <Button Content="PDF" Click="PdfButton_Click"
            Background="#F44336" Foreground="White"
            Padding="12,8" FontWeight="SemiBold" BorderThickness="0"
            MinWidth="100" Margin="5,0" Cursor="Hand"/>
    <Button Content="Excel" Click="ExcelButton_Click"
            Background="#4CAF50" Foreground="White"
            Padding="12,8" FontWeight="SemiBold" BorderThickness="0"
            MinWidth="100" Margin="5,0" Cursor="Hand"/>
</StackPanel>
```

2. **Add event handlers to code-behind**:
```csharp
using Fruittrack.Utilities;

private void PrintButton_Click(object sender, RoutedEventArgs e)
{
    ExportUtilities.PrintPage(this, "Page Title");
}

private void PdfButton_Click(object sender, RoutedEventArgs e)
{
    var filePath = ExportUtilities.GetSaveFilePath("filename.pdf", "PDF files (*.pdf)|*.pdf");
    if (!string.IsNullOrEmpty(filePath))
    {
        ExportUtilities.ExportToPdf(this, filePath, "Page Title");
    }
}

private void ExcelButton_Click(object sender, RoutedEventArgs e)
{
    var filePath = ExportUtilities.GetSaveFilePath("filename.xlsx", "Excel files (*.xlsx)|*.xlsx");
    if (!string.IsNullOrEmpty(filePath))
    {
        ExportUtilities.ExportDataGridToExcel(YourDataGrid, filePath, "Sheet Name");
    }
}
```

## Technical Details

### ExportUtilities Class Methods

#### `PrintPage(FrameworkElement element, string title)`
- Captures the visual element and sends it to the printer
- Shows print dialog for user configuration
- Handles errors gracefully

#### `ExportToPdf(FrameworkElement element, string filePath, string title)`
- Creates PDF document with proper formatting
- Adds title and date to the PDF
- Converts the element to an image and embeds it
- Supports Arabic text and right-to-left layout

#### `ExportToExcel(DataTable dataTable, string filePath, string sheetName)`
- Creates Excel file with formatted headers
- Auto-fits columns for better readability
- Supports Arabic text

#### `ExportDataGridToExcel(DataGrid dataGrid, string filePath, string sheetName)`
- Extracts data from WPF DataGrid
- Handles different column types
- Preserves header information

#### `GetSaveFilePath(string defaultName, string filter)`
- Shows standard save file dialog
- Returns selected file path or null if cancelled

## Error Handling
All export functions include comprehensive error handling:
- File access errors
- Printer errors
- Memory issues
- User-friendly error messages in Arabic

## File Naming Convention
- PDF files: `{page_name}.pdf`
- Excel files: `{page_name}.xlsx`
- Default names are in Arabic with underscores

## Dependencies
- **iTextSharp**: For PDF generation
- **EPPlus**: For Excel file creation
- **System.Printing**: For printing functionality
- **System.Windows.Media.Imaging**: For visual capture

## Notes
- All export functions are thread-safe
- PDF files include proper Arabic text support
- Excel files are compatible with Microsoft Excel and LibreOffice
- Print functionality works with any Windows printer
- File dialogs respect user's language settings 