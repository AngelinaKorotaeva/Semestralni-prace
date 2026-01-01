using System.Windows;
using System.Windows.Controls;
using SkolniJidelna.Data;
using SkolniJidelna.Services;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna
{
    public partial class AdminPanel : Window
    {
        private string Email;
        public AdminPanel(string email)
        {
            InitializeComponent();
            Loaded += AdminPanel_Loaded;

            this.Email = email;


            var db = new AppDbContext();
            var vm = new AdminViewModel(db);
            this.DataContext = vm;
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


        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is AdminViewModel vm && sender is ListBox lb)
            {
                if (vm.SelectedEntityType?.Name == "Studenti")
                {
                    vm.SelectedClass = lb.SelectedItem as string;
                }
                else if (vm.SelectedEntityType?.Name == "Pracovníci")
                {
                    vm.SelectedPosition = lb.SelectedItem as string;
                }
                else if (vm.SelectedEntityType?.Name == "Jídla")
                {
                    vm.SelectedFoodCategory = lb.SelectedItem as string;
                }
                else if (vm.SelectedEntityType?.Name == "Alergie a omezení")
                {
                    vm.SelectedDietType = lb.SelectedItem as string;
                } else if (vm.SelectedEntityType?.Name == "Menu")
                {
                    vm.SelectedMenuType = lb.SelectedItem as string;
                }
                else if (vm.SelectedEntityType?.Name == "Objednavky")
                {
                    vm.SelectedOrderState = lb.SelectedItem as string;
                }
                else
                {
                    vm.OnSelectedItemChanged(lb.SelectedItem as ItemViewModel);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aw = new AdminProfileWindow(Email);
                aw.Show();
                this.Close();
            }
            catch
            {

            }
        }

        private async void UpravitButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminViewModel vm && vm.SelectedItem != null)
            {
                var editDialog = new EditDialogWindow(vm.Properties);
                if (editDialog.ShowDialog() == true)
                {
                    await vm.SaveAsync();
                }
            }
        }

        private async void PridatButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AdminViewModel vm) return;

            // Delegate creation flow to ViewModel (MVVM): it will open dialog, call stored procedure and refresh list
            await vm.AddNewStravnikAsync(this);
        }

        private void SystemovyKatalog_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminViewModel vm)
            {
                var wnd = new SystemovyKatalogWindow(vm.DbContext);
                wnd.ShowDialog();
            }
        }
    }
}