using System.Windows;
using System.Windows.Controls;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna
{
    public partial class AdminPanel : Window
    {
        public AdminPanel()
        {
            InitializeComponent();
            Loaded += AdminPanel_Loaded;
        }

        private async void AdminPanel_Loaded(object? sender, RoutedEventArgs e)
        {
            // Pokud je DataContext AdminViewModel, načteme položky při otevření
            if (DataContext is AdminViewModel vm)
            {
                await vm.LoadEntityTypesAsync();
            }
        }

        // XAML combobox používá SelectionChanged="FilterChanged"
        private async void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is AdminViewModel vm)
            {
                await vm.LoadItemsForSelectedEntityAsync();
            }
        }

        // Pokud později přidáte SelectionChanged na ListBox, použijte tento handler
        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is AdminViewModel vm && sender is ListBox lb)
            {
                vm.OnSelectedItemChanged(lb.SelectedItem as ItemViewModel);
            }
        }
    }
}