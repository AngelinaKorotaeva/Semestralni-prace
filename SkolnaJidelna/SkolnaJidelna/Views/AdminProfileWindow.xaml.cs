using System;
using System.Windows;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna
{
    /// <summary>
    /// Interakční logika pro AdminProfileWindow.xaml
    /// </summary>
    public partial class AdminProfileWindow : Window
    {
        private string Email;
        public AdminProfileWindow(string email)
        {
            InitializeComponent();

            this.Email = email;

            if (!string.IsNullOrWhiteSpace(email))
            {
                var vm = new AdminProfileViewModel(email);
                this.DataContext = vm;
            }
        }

        //public AdminProfileWindow(string adminEmail) : this()
        //{
        //    if (!string.IsNullOrWhiteSpace(adminEmail))
        //    {
        //        var vm = new AdminProfileViewModel(adminEmail);
        //        DataContext = vm;
        //
        //        // No debug popup on load
        //    }
        //}

        private void RechargeBalanceButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new MenuWindow(Email);
            menu.Show();
            this.Close();
        }

        private void CreateOrderButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OrderHistoryButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {
            var adminPanel = new AdminPanel(Email);
            adminPanel.Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var main = App.Services.GetService(typeof(MainWindow)) as MainWindow;
                if (main != null)
                {
                    // ensure DataContext is set to Login VM
                    if (main.DataContext is not MainWindowViewModel)
                    {
                        var vm = App.Services.GetService(typeof(MainWindowViewModel)) as MainWindowViewModel;
                        if (vm != null)
                        {
                            main.DataContext = vm;
                        }
                    }
                    main.Show();
                }
                else
                {
                    // fallback if DI instance not available
                    var mw = new MainWindow();
                    var vm = App.Services.GetService(typeof(MainWindowViewModel)) as MainWindowViewModel;
                    if (vm != null)
                    {
                        mw.DataContext = vm;
                    }
                    mw.Show();
                }
            }
            catch
            {
                // fallback
                var mw = new MainWindow();
                var vm = App.Services.GetService(typeof(MainWindowViewModel)) as MainWindowViewModel;
                if (vm != null)
                {
                    mw.DataContext = vm;
                }
                mw.Show();
            }
            this.Close();
        }

        private async void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AdminProfileViewModel vm)
            {
                await vm.SaveChangesAsync();
            }
        }

        private async void ChangePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is AdminProfileViewModel vm)
            {
                await vm.ChangePhotoAsync();
            }
        }
    }
}