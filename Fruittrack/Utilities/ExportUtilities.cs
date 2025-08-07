using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using Wp = DocumentFormat.OpenXml.Wordprocessing;

namespace Fruittrack.Utilities
{
    public static class ExportUtilities
    {
        public static void PrintPage(FrameworkElement element, string title = "طباعة")
        {
            try
            {
                // Direct printing without preview
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var printDocument = new FixedDocument();
                    var pageContent = new PageContent();
                    var fixedPage = new FixedPage();

                    // Create a visual brush to capture the element
                    var visualBrush = new VisualBrush(element)
                    {
                        Stretch = Stretch.Uniform
                    };

                    var rectangle = new System.Windows.Shapes.Rectangle
                    {
                        Fill = visualBrush,
                        Width = printDialog.PrintableAreaWidth,
                        Height = printDialog.PrintableAreaHeight
                    };

                    fixedPage.Children.Add(rectangle);
                    fixedPage.Width = printDialog.PrintableAreaWidth;
                    fixedPage.Height = printDialog.PrintableAreaHeight;

                    pageContent.Child = fixedPage;
                    printDocument.Pages.Add(pageContent);

                    printDialog.PrintDocument(printDocument.DocumentPaginator, title);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ExportToPdf(FrameworkElement element, string filePath, string title = "تقرير")
        {
            try
            {
                // Create PDF document with RTL support
                using var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 50, 50, 50, 50);
                using var writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                document.Open();

                // Try to use Arabic font, fallback to Arial if not available
                BaseFont baseFont;
                try
                {
                    // Try to use a system Arabic font
                    baseFont = BaseFont.CreateFont("C:\\Windows\\Fonts\\arial.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                }
                catch
                {
                    // Fallback to default font
                    baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.EMBEDDED);
                }

                var titleFont = new iTextSharp.text.Font(baseFont, 18, iTextSharp.text.Font.BOLD);
                var titleParagraph = new iTextSharp.text.Paragraph(title, titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20f
                };
                document.Add(titleParagraph);

                // Add date with RTL alignment
                var dateFont = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.NORMAL);
                var dateParagraph = new iTextSharp.text.Paragraph($"التاريخ: {DateTime.Now:dd/MM/yyyy}", dateFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 20f
                };
                document.Add(dateParagraph);

                // Convert element to image and add to PDF
                var bitmap = RenderElementToBitmap(element);
                using var imageStream = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(imageStream);
                imageStream.Position = 0;

                var image = iTextSharp.text.Image.GetInstance(imageStream.ToArray());
                image.ScaleToFit(500, 700);
                image.Alignment = Element.ALIGN_CENTER;
                document.Add(image);

                document.Close();
                MessageBox.Show($"تم حفظ الملف بنجاح في: {filePath}", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ ملف PDF: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static DataTable ExtractDataFromElement(FrameworkElement element)
        {
            try
            {
                // Try to extract data from DataGrid
                if (element is DataGrid dataGrid)
                {
                    var dataTable = new DataTable();
                    var usedColumnNames = new HashSet<string>();

                    // Add columns with duplicate handling
                    foreach (var column in dataGrid.Columns)
                    {
                        var columnName = column.Header?.ToString() ?? $"Column{column.DisplayIndex}";
                        
                        // Handle duplicate column names
                        var originalName = columnName;
                        var counter = 1;
                        while (usedColumnNames.Contains(columnName))
                        {
                            columnName = $"{originalName}_{counter}";
                            counter++;
                        }
                        
                        usedColumnNames.Add(columnName);
                        dataTable.Columns.Add(columnName);
                    }

                    // Add rows - improved data extraction
                    foreach (var item in dataGrid.Items)
                    {
                        var row = dataTable.NewRow();
                        for (int i = 0; i < dataGrid.Columns.Count; i++)
                        {
                            var column = dataGrid.Columns[i];
                            var cellValue = column.GetCellContent(item);
                            
                            string cellText = "";
                            
                            // Try multiple approaches to get cell text
                            if (cellValue is TextBlock textBlock)
                            {
                                cellText = textBlock.Text;
                            }
                            else if (cellValue is ContentPresenter contentPresenter)
                            {
                                // Try to get content from ContentPresenter
                                if (contentPresenter.Content is TextBlock tb)
                                {
                                    cellText = tb.Text;
                                }
                                else
                                {
                                    cellText = contentPresenter.Content?.ToString() ?? "";
                                }
                            }
                            else if (cellValue is FrameworkElement fe)
                            {
                                // Try to find TextBlock within the element
                                var foundTextBlock = FindVisualChild<TextBlock>(fe);
                                if (foundTextBlock != null)
                                {
                                    cellText = foundTextBlock.Text;
                                }
                                else
                                {
                                    cellText = fe.ToString();
                                }
                            }
                            else
                            {
                                cellText = cellValue?.ToString() ?? "";
                            }
                            
                            // If we still don't have text, try to get it from the binding
                            if (string.IsNullOrEmpty(cellText) && column is DataGridBoundColumn boundColumn)
                            {
                                try
                                {
                                    var binding = boundColumn.Binding as System.Windows.Data.Binding;
                                    if (binding != null && item != null)
                                    {
                                        var property = item.GetType().GetProperty(binding.Path.Path);
                                        if (property != null)
                                        {
                                            var value = property.GetValue(item);
                                            cellText = value?.ToString() ?? "";
                                        }
                                    }
                                }
                                catch
                                {
                                    // Ignore binding errors
                                }
                            }
                            
                            row[i] = cellText;
                        }
                        dataTable.Rows.Add(row);
                    }

                    return dataTable;
                }

                // Try to extract data from other common controls
                if (element is Grid grid)
                {
                    // Look for DataGrid within the grid
                    var gridDataGrid = FindVisualChild<DataGrid>(grid);
                    if (gridDataGrid != null)
                    {
                        return ExtractDataFromElement(gridDataGrid);
                    }
                }

                // Try to extract data from ScrollViewer (common container for DataGrid)
                if (element is ScrollViewer scrollViewer)
                {
                    var scrollDataGrid = FindVisualChild<DataGrid>(scrollViewer);
                    if (scrollDataGrid != null)
                    {
                        return ExtractDataFromElement(scrollDataGrid);
                    }
                }

                // Try to extract data from any container
                var containerDataGrid = FindVisualChild<DataGrid>(element);
                if (containerDataGrid != null)
                {
                    return ExtractDataFromElement(containerDataGrid);
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في استخراج البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;
                
                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }
            return null;
        }

        public static void ExportToExcel(DataTable dataTable, string filePath, string sheetName = "Sheet1")
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add(sheetName);

                // Add headers
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = dataTable.Columns[i].ColumnName;
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Add data
                for (int row = 0; row < dataTable.Rows.Count; row++)
                {
                    for (int col = 0; col < dataTable.Columns.Count; col++)
                    {
                        worksheet.Cells[row + 2, col + 1].Value = dataTable.Rows[row][col];
                    }
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Save file
                package.SaveAs(new FileInfo(filePath));
                MessageBox.Show($"تم حفظ الملف بنجاح في: {filePath}", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ ملف Excel: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void ExportDataGridToExcel(DataGrid dataGrid, string filePath, string sheetName = "Sheet1")
        {
            try
            {
                var dataTable = new DataTable();
                var usedColumnNames = new HashSet<string>();

                // Add columns with duplicate handling
                foreach (var column in dataGrid.Columns)
                {
                    var columnName = column.Header?.ToString() ?? $"Column{column.DisplayIndex}";
                    
                    // Handle duplicate column names
                    var originalName = columnName;
                    var counter = 1;
                    while (usedColumnNames.Contains(columnName))
                    {
                        columnName = $"{originalName}_{counter}";
                        counter++;
                    }
                    
                    usedColumnNames.Add(columnName);
                    dataTable.Columns.Add(columnName);
                }

                // Add rows
                foreach (var item in dataGrid.Items)
                {
                    var row = dataTable.NewRow();
                    for (int i = 0; i < dataGrid.Columns.Count; i++)
                    {
                        var column = dataGrid.Columns[i];
                        var cellValue = column.GetCellContent(item);
                        if (cellValue is TextBlock textBlock)
                        {
                            row[i] = textBlock.Text;
                        }
                        else
                        {
                            row[i] = cellValue?.ToString() ?? "";
                        }
                    }
                    dataTable.Rows.Add(row);
                }

                ExportToExcel(dataTable, filePath, sheetName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تصدير الجدول: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static RenderTargetBitmap RenderElementToBitmap(FrameworkElement element)
        {
            var renderTarget = new RenderTargetBitmap(
                (int)element.ActualWidth,
                (int)element.ActualHeight,
                96, 96, PixelFormats.Pbgra32);

            renderTarget.Render(element);
            return renderTarget;
        }

        public static string GetSaveFilePath(string defaultName, string filter)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = defaultName,
                Filter = filter
            };

            return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;
        }
    }
} 