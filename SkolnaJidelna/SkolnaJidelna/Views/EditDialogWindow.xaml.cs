using System.Collections.ObjectModel;
using System.Windows;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna
{
    public partial class EditDialogWindow : Window
    {
        public EditDialogWindow(ObservableCollection<PropertyViewModel> properties)
        {
            InitializeComponent();
            DataContext = properties;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}