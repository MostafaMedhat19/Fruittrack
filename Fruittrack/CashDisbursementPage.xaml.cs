using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Fruittrack.ViewModels;
using Fruittrack.Utilities;

namespace Fruittrack
{
    /// <summary>
    /// Interaction logic for CashDisbursementPage.xaml
    /// </summary>
    public partial class CashDisbursementPage : Page
    {
        public CashDisbursementPage()
        {
            InitializeComponent();
            
            // Get the DbContext from the application
            var dbContext = ((App)App.Current).DbContext;
            DataContext = new CashDisbursementPageViewModel(dbContext);
        }

        private void PrintButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ExportUtilities.ExportToTemporaryPdfAndOpen(this, "صرف نقدية");
        }

        private void PdfButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var filePath = ExportUtilities.GetSaveFilePath("صرف_نقدية.pdf", "PDF files (*.pdf)|*.pdf");
            if (!string.IsNullOrEmpty(filePath))
            {
                ExportUtilities.ExportToPdf(this, filePath, "صرف نقدية");
            }
        }

        // Event handler for numeric-only input validation
        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Get current text and the position where new text will be inserted
            string currentText = textBox.Text;
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;

            // Simulate what the text would be after the input
            string newText = currentText.Remove(selectionStart, selectionLength).Insert(selectionStart, e.Text);

            // Allow only digits and one decimal separator (. or ,)
            bool isValidInput = true;
            bool hasDecimalSeparator = false;

            foreach (char c in newText)
            {
                if (char.IsDigit(c))
                {
                    continue;
                }
                else if ((c == '.' || c == ',') && !hasDecimalSeparator)
                {
                    hasDecimalSeparator = true;
                }
                else
                {
                    isValidInput = false;
                    break;
                }
            }

            // Also validate that it can be parsed as decimal
            if (isValidInput && !string.IsNullOrEmpty(newText))
            {
                // Replace comma with dot for parsing
                string testText = newText.Replace(',', '.');
                isValidInput = decimal.TryParse(testText, out _);
            }

            e.Handled = !isValidInput;
        }

        // Prevent paste operations that contain non-numeric characters
        private void CommandManager_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    string clipboardText = Clipboard.GetText();
                    if (!string.IsNullOrEmpty(clipboardText))
                    {
                        // Check if clipboard contains only numeric characters
                        string testText = clipboardText.Replace(',', '.');
                        bool isNumeric = decimal.TryParse(testText, out _);
                        if (!isNumeric)
                        {
                            e.Handled = true; // Cancel paste operation
                        }
                    }
                }
            }
        }
    }
}
