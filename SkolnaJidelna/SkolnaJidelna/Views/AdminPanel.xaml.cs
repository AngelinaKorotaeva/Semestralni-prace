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
        private string _originalAdminEmail;
        public AdminPanel(string email)
        {
            InitializeComponent();
            Loaded += AdminPanel_Loaded;

            this.Email = email;
            this._originalAdminEmail = email;


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

        private void SwitchUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminViewModel vm && vm.SelectedItem != null)
            {
                // selected item entity should contain Stravnik with Email property for students/workers
                var entity = vm.SelectedItem.Entity;
                string targetEmail = null;

                // try common patterns
                var type = entity.GetType();
                var propEmail = type.GetProperty("Email");
                if (propEmail != null)
                {
                    var val = propEmail.GetValue(entity);
                    targetEmail = val?.ToString();
                }
                else
                {
                    // maybe nested Stravnik property
                    var stravnikProp = type.GetProperty("Stravnik");
                    if (stravnikProp != null)
                    {
                        var stravnik = stravnikProp.GetValue(entity);
                        if (stravnik != null)
                        {
                            var eprop = stravnik.GetType().GetProperty("Email");
                            if (eprop != null) targetEmail = eprop.GetValue(stravnik)?.ToString();
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(targetEmail))
                {
                    MessageBox.Show("Nelze přepnout: vybraný záznam neobsahuje e-mail.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Open UserProfileWindow for targetEmail, but keep admin's original email to allow return
                try
                {
                    var userWin = new UserProfileWindow(targetEmail);
                    // store original admin email on the window's Tag to allow return
                    userWin.Tag = this._originalAdminEmail;
                    userWin.Show();
                    this.Close();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Nelze otevřít profil uživatele: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Vyberte uživatele, na kterého chcete přepnout.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}