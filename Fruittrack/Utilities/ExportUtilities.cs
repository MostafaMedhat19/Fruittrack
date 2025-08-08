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
using System.Windows.Threading;
using System.Globalization;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using Wp = DocumentFormat.OpenXml.Wordprocessing;
// Add draw for line separator
using iTextSharp.text.pdf.draw;

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

        public static void ExportToPdf(
            FrameworkElement element,
            string filePath,
            string title = "تقرير",
            string companyName = "شركة فروت تراك",
            string logoPath = null)
        {
            try
            {
                using (var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4, 36, 36, 48, 48))
                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    var writer = PdfWriter.GetInstance(document, fs);
                    writer.PageEvent = new SimpleFooterEvent();

                    document.Open();

                    // 1) Build an Arabic-correct header as a WPF visual and add as image
                    var header = BuildHeaderVisual(companyName, title, logoPath);
                    var headerBitmap = RenderOffscreenElementToBitmap(header);
                    using (var headerStream = new MemoryStream())
                    {
                        var headerEncoder = new PngBitmapEncoder();
                        headerEncoder.Frames.Add(BitmapFrame.Create(headerBitmap));
                        headerEncoder.Save(headerStream);
                        headerStream.Position = 0;
                        var headerImg = iTextSharp.text.Image.GetInstance(headerStream.ToArray());
                        headerImg.ScaleToFit((float)(document.PageSize.Width - document.LeftMargin - document.RightMargin), 120f);
                        headerImg.Alignment = Element.ALIGN_CENTER;
                        document.Add(headerImg);
                    }

                    document.Add(new Chunk(new LineSeparator(0.5f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -2)));
                    document.Add(Chunk.NEWLINE);

                    // 2) If there is a DataGrid inside the element, try to export as real PDF table (Arabic-safe)
                    var dataGrid = FindVisualChild<DataGrid>(element);
                    List<(DataGridColumn column, Visibility originalVisibility)> modifiedColumns = new List<(DataGridColumn, Visibility)>();
                    if (dataGrid != null)
                    {
                        foreach (var col in dataGrid.Columns)
                        {
                            var headerText = col.Header != null ? col.Header.ToString() : string.Empty;
                            if (!string.IsNullOrWhiteSpace(headerText) && headerText.Trim() == "الإجراءات")
                            {
                                modifiedColumns.Add((col, col.Visibility));
                                col.Visibility = Visibility.Collapsed;
                            }
                        }
                        dataGrid.UpdateLayout();
                    }

                    // 3) Prefer real table export with Arabic font; fallback to image capture if extraction fails
                    bool tableAdded = false;
                    if (dataGrid != null)
                    {
                    var dataTable = ExtractDataFromElement(dataGrid);
                        if (dataTable != null && dataTable.Columns.Count > 0)
                        {
                            var bf = TryLoadArabicBaseFont();
                            var fontHeader = new iTextSharp.text.Font(bf, 11, iTextSharp.text.Font.BOLD, BaseColor.WHITE);
                            var fontCell = new iTextSharp.text.Font(bf, 10, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);

                            var pdfTable = new PdfPTable(dataTable.Columns.Count)
                            {
                                WidthPercentage = 100
                            };
                            pdfTable.RunDirection = PdfWriter.RUN_DIRECTION_RTL;

                            // Headers
                            foreach (DataColumn col in dataTable.Columns)
                            {
                                var headerCell = new PdfPCell(new Phrase(col.ColumnName, fontHeader))
                                {
                                    BackgroundColor = new BaseColor(23, 42, 58), // dark header
                                    HorizontalAlignment = Element.ALIGN_CENTER,
                                    VerticalAlignment = Element.ALIGN_MIDDLE,
                                    PaddingTop = 6f,
                                    PaddingBottom = 6f
                                };
                                headerCell.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                                pdfTable.AddCell(headerCell);
                            }

                            // Rows
                            bool odd = false;
                            foreach (DataRow row in dataTable.Rows)
                            {
                                odd = !odd;
                                for (int i = 0; i < dataTable.Columns.Count; i++)
                                {
                                    string text = row[i] != null ? row[i].ToString() : string.Empty;
                                    var cell = new PdfPCell(new Phrase(text, fontCell))
                                    {
                                        BackgroundColor = odd ? new BaseColor(247, 250, 252) : BaseColor.WHITE,
                                        HorizontalAlignment = Element.ALIGN_RIGHT,
                                        VerticalAlignment = Element.ALIGN_MIDDLE,
                                        PaddingTop = 4f,
                                        PaddingBottom = 4f,
                                        PaddingLeft = 4f,
                                        PaddingRight = 4f
                                    };
                                    cell.RunDirection = PdfWriter.RUN_DIRECTION_RTL;
                                    pdfTable.AddCell(cell);
                                }
                            }

                            document.Add(pdfTable);
                            tableAdded = true;
                        }
                    }

                    if (!tableAdded)
                    {
                        // Fallback: capture image(s)
                        BitmapSource contentBitmap;
                        if (dataGrid != null)
                        {
                            contentBitmap = CaptureDataGridAllColumns(dataGrid);
                        }
                        else
                        {
                            contentBitmap = RenderElementToBitmap(element);
                        }

                        // Available content area in points
                        double contentWidthPt = document.PageSize.Width - document.LeftMargin - document.RightMargin;
                        double contentHeightPt = document.PageSize.Height - document.TopMargin - document.BottomMargin - 140; // leave space for header
                        // Convert to pixels at 96 DPI (1pt = 1/72 inch)
                        int pageWidthPx = (int)Math.Max(1, Math.Round(contentWidthPt / 72.0 * 96.0));

                        int totalSegments = (int)Math.Ceiling((double)contentBitmap.PixelWidth / pageWidthPx);
                        for (int seg = 0; seg < Math.Max(1, totalSegments); seg++)
                        {
                            if (seg > 0)
                            {
                                document.NewPage();
                                // Re-add header for each subsequent page (Arabic-safe WPF header)
                                var header2 = BuildHeaderVisual(companyName, title, logoPath);
                                var headerBitmap2 = RenderOffscreenElementToBitmap(header2);
                                using (var headerStream2 = new MemoryStream())
                                {
                                    var headerEncoder2 = new PngBitmapEncoder();
                                    headerEncoder2.Frames.Add(BitmapFrame.Create(headerBitmap2));
                                    headerEncoder2.Save(headerStream2);
                                    headerStream2.Position = 0;
                                    var headerImg2 = iTextSharp.text.Image.GetInstance(headerStream2.ToArray());
                                    headerImg2.ScaleToFit((float)contentWidthPt, 120f);
                                    headerImg2.Alignment = Element.ALIGN_CENTER;
                                    document.Add(headerImg2);
                                    document.Add(new Chunk(new LineSeparator(0.5f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -2)));
                                    document.Add(Chunk.NEWLINE);
                                }
                            }

                            int x = seg * pageWidthPx;
                            int width = Math.Min(pageWidthPx, contentBitmap.PixelWidth - x);
                            if (width <= 0)
                            {
                                break;
                            }
                            var cropped = new CroppedBitmap(contentBitmap as BitmapSource, new Int32Rect(x, 0, width, contentBitmap.PixelHeight));
                            using (var contentStream = new MemoryStream())
                            {
                                var encoder = new PngBitmapEncoder();
                                encoder.Frames.Add(BitmapFrame.Create(cropped));
                                encoder.Save(contentStream);
                                contentStream.Position = 0;

                                var image = iTextSharp.text.Image.GetInstance(contentStream.ToArray());
                                image.ScaleToFit((float)contentWidthPt, (float)contentHeightPt);
                                image.Alignment = Element.ALIGN_CENTER;
                                document.Add(image);
                            }
                        }
                    }

                    // 4) Restore any modified columns
                    if (modifiedColumns.Count > 0)
                    {
                        foreach (var pair in modifiedColumns)
                        {
                            pair.column.Visibility = pair.originalVisibility;
                        }
                        dataGrid.UpdateLayout();
                    }

                    document.Close();
                }
                MessageBox.Show($"تم حفظ الملف بنجاح في: {filePath}", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ ملف PDF: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Capture the DataGrid showing all columns by scrolling horizontally and stitching images
        private static BitmapSource CaptureDataGridAllColumns(DataGrid dataGrid)
        {
            try
            {
                dataGrid.UpdateLayout();
                var scrollViewer = FindVisualChild<ScrollViewer>(dataGrid) ?? FindVisualParent<ScrollViewer>(dataGrid);
                if (scrollViewer == null || scrollViewer.ExtentWidth <= scrollViewer.ViewportWidth)
                {
                    return RenderElementToBitmap(dataGrid);
                }

                var originalOffset = scrollViewer.HorizontalOffset;
                var slices = new List<BitmapSource>();

                // Scroll through the horizontal extent and capture slices
                double step = Math.Max(1, scrollViewer.ViewportWidth - 2); // small overlap to avoid gaps
                for (double offset = 0; offset < scrollViewer.ExtentWidth - 0.5; offset += step)
                {
                    scrollViewer.ScrollToHorizontalOffset(offset);
                    // Force layout/render before capture
                    dataGrid.Dispatcher.Invoke(() => { dataGrid.UpdateLayout(); scrollViewer.UpdateLayout(); }, DispatcherPriority.Render);
                    slices.Add(RenderElementToBitmap(scrollViewer));
                }

                // Restore original offset
                scrollViewer.ScrollToHorizontalOffset(originalOffset);
                dataGrid.Dispatcher.Invoke(() => { dataGrid.UpdateLayout(); }, DispatcherPriority.Render);

                // Stitch slices horizontally
                int totalWidth = slices.Sum(s => s.PixelWidth);
                int maxHeight = slices.Max(s => s.PixelHeight);
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    double x = 0;
                    foreach (var slice in slices)
                    {
                        dc.DrawImage(slice, new System.Windows.Rect(x, 0, slice.PixelWidth, slice.PixelHeight));
                        x += slice.PixelWidth;
                    }
                }
                var rtb = new RenderTargetBitmap(totalWidth, maxHeight, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(dv);
                return rtb;
            }
            catch
            {
                return RenderElementToBitmap(dataGrid);
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
                        
                        // Skip actions column explicitly
                        if (!string.IsNullOrEmpty(columnName) && columnName.Trim() == "الإجراءات")
                        {
                            continue;
                        }

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
                        // Skip placeholder rows
                        if (object.ReferenceEquals(item, System.Windows.Data.CollectionView.NewItemPlaceholder))
                            continue;

                        var row = dataTable.NewRow();
                        int targetColIndex = 0;
                        for (int i = 0; i < dataGrid.Columns.Count; i++)
                        {
                            var column = dataGrid.Columns[i];
                            var headerText = column.Header?.ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(headerText) && headerText.Trim() == "الإجراءات")
                            {
                                // skip actions column in export
                                continue;
                            }

                            string cellText = "";

                            // 1) Prefer bound value via column binding (handles off-screen cells, notes, etc.)
                            if (column is DataGridBoundColumn boundColumn)
                            {
                                var binding = boundColumn.Binding as System.Windows.Data.Binding;
                                if (binding != null && item != null && binding.Path != null && !string.IsNullOrEmpty(binding.Path.Path))
                                {
                                    // Try to get raw value to apply StringFormat if present
                                    object rawValue = GetObjectByPropertyPath(item, binding.Path.Path);
                                    if (rawValue != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(binding.StringFormat))
                                        {
                                            try
                                            {
                                                // If it's a DateTime and StringFormat is a plain custom format (e.g., dd/MM/yyyy)
                                                if (rawValue is DateTime dt && !binding.StringFormat.Contains("{") && !binding.StringFormat.Contains("}"))
                                                {
                                                    cellText = dt.ToString(binding.StringFormat, CultureInfo.CurrentCulture);
                                                }
                                                else
                                                {
                                                    // Generic formatting fallback
                                                    string fmt = binding.StringFormat.Contains("{") ? binding.StringFormat : $"{{0:{binding.StringFormat}}}";
                                                    cellText = string.Format(CultureInfo.CurrentCulture, fmt, rawValue);
                                                }
                                            }
                                            catch
                                            {
                                                cellText = rawValue.ToString();
                                            }
                                        }
                                        else if (!string.IsNullOrEmpty(headerText) && headerText.Trim() == "التاريخ" && rawValue is DateTime dateOnly)
                                        {
                                            // Ensure date without time for تاريخ column
                                            cellText = dateOnly.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
                                        }
                                        else
                                        {
                                            cellText = rawValue.ToString();
                                        }
                                    }
                                }
                            }

                            // 2) Fallback to visual content if needed
                            if (string.IsNullOrEmpty(cellText))
                            {
                                var cellValue = column.GetCellContent(item);
                                if (cellValue is TextBlock textBlock)
                                {
                                    cellText = textBlock.Text;
                                }
                                else if (cellValue is ContentPresenter contentPresenter)
                                {
                                    var tb = FindVisualChild<TextBlock>(contentPresenter);
                                    if (tb != null)
                                    {
                                        cellText = tb.Text;
                                    }
                                    else
                                    {
                                        // As a robust fallback for Notes template column
                                        if (!string.IsNullOrEmpty(headerText) && headerText.Trim() == "الملاحظات")
                                        {
                                            try
                                            {
                                                var notesProp = item.GetType().GetProperty("Notes");
                                                if (notesProp != null)
                                                {
                                                    var value = notesProp.GetValue(item);
                                                    cellText = value?.ToString() ?? string.Empty;
                                                }
                                                else
                                                {
                                                    cellText = string.Empty;
                                                }
                                            }
                                            catch { cellText = string.Empty; }
                                        }
                                        else
                                        {
                                            // Avoid dumping type name; keep empty fallback
                                            cellText = string.Empty;
                                        }
                                    }
                                }
                                else if (cellValue is FrameworkElement fe)
                                {
                                    var foundTextBlock = FindVisualChild<TextBlock>(fe);
                                    if (foundTextBlock != null)
                                    {
                                        cellText = foundTextBlock.Text;
                                    }
                                }
                            }

                            row[targetColIndex] = cellText;
                            targetColIndex++;
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
                var containerDataGrid2 = FindVisualChild<DataGrid>(element);
                if (containerDataGrid2 != null)
                {
                    return ExtractDataFromElement(containerDataGrid2);
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في استخراج البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // Helper: get nested property value by path like "Truck.TruckNumber"
        private static string GetValueByPropertyPath(object source, string path)
        {
            try
            {
                if (source == null || string.IsNullOrWhiteSpace(path))
                    return null;

                object current = source;
                var parts = path.Split('.')
                                 .Select(p => p.Trim())
                                 .Where(p => p.Length > 0);
                foreach (var part in parts)
                {
                    var type = current.GetType();
                    var prop = type.GetProperty(part);
                    if (prop == null)
                        return null;
                    current = prop.GetValue(current, null);
                    if (current == null)
                        return null;
                }
                return current?.ToString();
            }
            catch
            {
                return null;
            }
        }

        // Helper: get raw object value by property path (without ToString) for formatting
        private static object GetObjectByPropertyPath(object source, string path)
        {
            try
            {
                if (source == null || string.IsNullOrWhiteSpace(path))
                    return null;

                object current = source;
                var parts = path.Split('.')
                                 .Select(p => p.Trim())
                                 .Where(p => p.Length > 0);
                foreach (var part in parts)
                {
                    var type = current.GetType();
                    var prop = type.GetProperty(part);
                    if (prop == null)
                        return null;
                    current = prop.GetValue(current, null);
                    if (current == null)
                        return null;
                }
                return current;
            }
            catch
            {
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

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject current = VisualTreeHelper.GetParent(child);
            while (current != null)
            {
                if (current is T match)
                    return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        public static void ExportToExcel(DataTable dataTable, string filePath, string sheetName = "Sheet1")
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
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
                }
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
                // Reuse the improved extractor so template columns like "الملاحظات" export real text
                var dataTable = ExtractDataFromElement(dataGrid);
                if (dataTable == null || dataTable.Columns.Count == 0)
                {
                    MessageBox.Show("لم يتم العثور على بيانات للتصدير.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Ensure تاريخ column has only date (in case it slipped through)
                int dateColIndex = -1;
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    if (string.Equals(dataTable.Columns[i].ColumnName?.Trim(), "التاريخ", StringComparison.Ordinal))
                    {
                        dateColIndex = i; break;
                    }
                }
                if (dateColIndex >= 0)
                {
                    foreach (DataRow r in dataTable.Rows)
                    {
                        var txt = r[dateColIndex]?.ToString();
                        if (DateTime.TryParse(txt, out var dt))
                        {
                            r[dateColIndex] = dt.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
                        }
                    }
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
            var width = Math.Max(1, (int)element.ActualWidth);
            var height = Math.Max(1, (int)element.ActualHeight);

            var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(element);
            return renderTarget;
        }

        // Render a newly created off-screen WPF element
        private static RenderTargetBitmap RenderOffscreenElementToBitmap(FrameworkElement element)
        {
            // Default size
            double width = element.Width > 0 ? element.Width : 800;
            double height = element.Height > 0 ? element.Height : 120;
            element.Measure(new Size(width, height));
            element.Arrange(new Rect(0, 0, width, height));
            element.UpdateLayout();

            var renderTarget = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
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

        // -------- Helpers for PDF branding --------
        private static PdfPTable BuildBrandedHeader(BaseFont baseFont, string title, string companyName, string logoPath)
        {
            var headerTable = new PdfPTable(2)
            {
                WidthPercentage = 100
            };
            headerTable.SetWidths(new float[] { 1f, 5f });

            // Try to load logo image
            var logoImg = TryLoadLogo(logoPath);
            PdfPCell logoCell;
            if (logoImg != null)
            {
                logoImg.ScaleToFit(60f, 60f);
                logoCell = new PdfPCell(logoImg)
                {
                    Border = Rectangle.NO_BORDER,
                    HorizontalAlignment = Element.ALIGN_LEFT,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 4f
                };
            }
            else
            {
                logoCell = new PdfPCell(new Phrase("")) { Border = Rectangle.NO_BORDER };
            }
            headerTable.AddCell(logoCell);

            // Right cell with company, title and date
            var companyFont = new iTextSharp.text.Font(baseFont, 16, iTextSharp.text.Font.BOLD, new BaseColor(16, 185, 129)); // teal
            var titleFont = new iTextSharp.text.Font(baseFont, 14, iTextSharp.text.Font.BOLD, BaseColor.BLACK);
            var infoFont = new iTextSharp.text.Font(baseFont, 11, iTextSharp.text.Font.NORMAL, BaseColor.GRAY);

            var paragraph = new iTextSharp.text.Paragraph { Alignment = Element.ALIGN_RIGHT };
            paragraph.Add(new Chunk(companyName + "\n", companyFont));
            paragraph.Add(new Chunk(title + "\n", titleFont));
            paragraph.Add(new Chunk($"تاريخ التصدير: {DateTime.Now:dd/MM/yyyy HH:mm}", infoFont));

            var rightCell = new PdfPCell(paragraph)
            {
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 4f
            };
            headerTable.AddCell(rightCell);

            return headerTable;
        }

        // Build a WPF visual header (Arabic-correct) to render as image
        private static FrameworkElement BuildHeaderVisual(string companyName, string title, string logoPath)
        {
            var grid = new Grid
            {
                Width = 800,
                Height = 120,
                Background = Brushes.Transparent,
                FlowDirection = FlowDirection.RightToLeft
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Star) });

            // Logo (right in RTL)
            var img = new System.Windows.Controls.Image
            {
                Width = 60,
                Height = 60,
                Margin = new Thickness(8),
                Stretch = Stretch.Uniform
            };
            var logo = TryLoadLogo(logoPath);
            if (logo != null)
            {
                // Convert iText image source file into BitmapImage for WPF
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    // Try fallback paths again for WPF image
                    string[] candidates = new[]
                    {
                        logoPath,
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "support-icon.jpg"),
                        Path.Combine(Directory.GetCurrentDirectory(), "Fruittrack", "Images", "support-icon.jpg"),
                        Path.Combine(Directory.GetCurrentDirectory(), "Images", "support-icon.jpg")
                    };
                    var found = candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
                    if (!string.IsNullOrEmpty(found))
                    {
                        bmp.UriSource = new Uri(found, UriKind.Absolute);
                        bmp.EndInit();
                        img.Source = bmp;
                    }
                }
                catch { }
            }
            Grid.SetColumn(img, 0);
            grid.Children.Add(img);

            // Texts
            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8)
            };
            var companyText = new TextBlock
            {
                Text = companyName,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129))
            };
            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            var dateText = new TextBlock
            {
                Text = $"تاريخ التصدير: {DateTime.Now:dd/MM/yyyy HH:mm}",
                FontSize = 13,
                Foreground = Brushes.Gray
            };
            stack.Children.Add(companyText);
            stack.Children.Add(titleText);
            stack.Children.Add(dateText);
            Grid.SetColumn(stack, 1);
            grid.Children.Add(stack);

            return grid;
        }

        private static iTextSharp.text.Image TryLoadLogo(string logoPath)
        {
            try
            {
                // If explicit path provided
                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                {
                    return iTextSharp.text.Image.GetInstance(logoPath);
                }

                // Common fallbacks: relative to current dir or base dir
                string[] candidates = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "support-icon.jpg"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Fruittrack", "Images", "support-icon.jpg"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Images", "support-icon.jpg")
                };

                var found = candidates.FirstOrDefault(File.Exists);
                if (found != null)
                {
                    return iTextSharp.text.Image.GetInstance(found);
                }
            }
            catch
            {
                // ignore logo load errors
            }
            return null;
        }

        // Load an Arabic-capable BaseFont from common Windows fonts
        private static BaseFont TryLoadArabicBaseFont()
        {
            string fontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            string[] candidates = new[]
            {
                Path.Combine(fontsDir, "trado.ttf"),        // Traditional Arabic
                Path.Combine(fontsDir, "arial.ttf"),        // Arial with Arabic
                Path.Combine(fontsDir, "segoeui.ttf"),      // Segoe UI
                Path.Combine(fontsDir, "Tahoma.ttf")        // Tahoma
            };
            foreach (var path in candidates)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        return BaseFont.CreateFont(path, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    }
                }
                catch { }
            }
            // Fallback: built-in Helvetica (may not shape Arabic correctly, but better than failing)
            return BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
        }

        private class SimpleFooterEvent : PdfPageEventHelper
        {
            public override void OnEndPage(PdfWriter writer, iTextSharp.text.Document document)
            {
                base.OnEndPage(writer, document);
                try
                {
                    var cb = writer.DirectContent;
                    cb.SaveState();

                    var footerText = $"صفحة {writer.PageNumber}";
                    var bf = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    cb.BeginText();
                    cb.SetFontAndSize(bf, 9);
                    // Center bottom
                    float x = (document.Left + document.Right) / 2;
                    float y = document.Bottom - 10;
                    cb.ShowTextAligned(Element.ALIGN_CENTER, footerText, x, y, 0);
                    cb.EndText();

                    cb.RestoreState();
                }
                catch
                {
                    // ignore footer errors
                }
            }
        }
    }
} 