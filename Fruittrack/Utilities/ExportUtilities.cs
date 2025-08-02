using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using iTextSharp.text;
using iTextSharp.text.pdf;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Fruittrack.Utilities
{
    public static class ExportUtilities
    {
        public static void PrintPage(FrameworkElement element, string title = "طباعة")
        {
            try
            {
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
                // Create PDF document
                using var document = new Document(PageSize.A4, 50, 50, 50, 50);
                using var writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
                document.Open();

                // Add title
                var titleFont = FontFactory.GetFont("Arial", 18, Font.BOLD);
                var titleParagraph = new iTextSharp.text.Paragraph(title, titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20f
                };
                document.Add(titleParagraph);

                // Add date
                var dateFont = FontFactory.GetFont("Arial", 12, Font.NORMAL);
                var dateParagraph = new iTextSharp.text.Paragraph($"التاريخ: {DateTime.Now:dd/MM/yyyy}", dateFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingAfter = 20f
                };
                document.Add(dateParagraph);

                // Convert element to image and add to PDF
                var bitmap = RenderElementToBitmap(element);
                using var stream = new MemoryStream();
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
                stream.Position = 0;

                var image = iTextSharp.text.Image.GetInstance(stream.ToArray());
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

                // Add columns
                foreach (var column in dataGrid.Columns)
                {
                    dataTable.Columns.Add(column.Header?.ToString() ?? $"Column{column.DisplayIndex}");
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