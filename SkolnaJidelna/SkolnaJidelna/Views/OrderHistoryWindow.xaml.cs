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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SkolniJidelna
{
    /// <summary>
    /// Interakční logika pro OrderHistoryWindow.xaml
    /// </summary>
    public partial class OrderHistoryWindow : Window
    {
        private string Email;
        public OrderHistoryWindow(string email)
        {
            InitializeComponent();

            this.Email = email;
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void OrderSelected(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
