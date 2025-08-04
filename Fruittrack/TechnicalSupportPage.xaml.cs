using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Fruittrack
{
    /// <summary>
    /// Interaction logic for TechnicalSupportPage.xaml
    /// </summary>
    public partial class TechnicalSupportPage : Page
    {
        public TechnicalSupportPage()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
           
                try
                {
                    Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("حدث خطأ أثناء فتح الرابط: " + ex.Message);
                }
            

            e.Handled = true;
        }

        

    }
}
