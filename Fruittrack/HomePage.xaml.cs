using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Fruittrack
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
            this.FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#Montserrat");

           
            if (!Resources.Contains("DropShadowEffect"))
            {
                Resources.Add("DropShadowEffect", new DropShadowEffect()
                {
                    BlurRadius = 8,
                    ShadowDepth = 2,
                    Color = Colors.Black,
                    Opacity = 0.2
                });
            }

            if (!Resources.Contains("TextShadowEffect"))
            {
                Resources.Add("TextShadowEffect", new DropShadowEffect()
                {
                    BlurRadius = 1,
                    ShadowDepth = 1,
                    Color = Colors.Black,
                    Opacity = 0.1
                });
            }
        }
        private void DigitOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {

            e.Handled = !int.TryParse(e.Text, out _);
        }
        private void MoveFocusOnInput(object sender, TextChangedEventArgs e)
        {
            TextBox currentBox = sender as TextBox;

            if (currentBox.Text.Length == currentBox.MaxLength)
            {
                currentBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new SupplyRecordPage());
        }
    }
}
