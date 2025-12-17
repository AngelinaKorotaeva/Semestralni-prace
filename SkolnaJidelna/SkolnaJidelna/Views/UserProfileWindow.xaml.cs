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
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using SkolniJidelna.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SkolniJidelna
{
    /// <summary>
    /// Interakční logika pro UserProfileWindow.xaml
    /// </summary>
    public partial class UserProfileWindow : Window
    {
        private bool TypStavnik = true;
        private string Email = "";
        public UserProfileWindow(string email)
        {
            InitializeComponent();
            this.Email = email;

            // Use MVVM: set DataContext to UserProfileViewModel
            var vm = new UserProfileViewModel(email);
            this.DataContext = vm;

            // Removed debug MessageBox on LoadFinished as requested
        }

        private void RechargeBalanceButton_Click(object sender, RoutedEventArgs e)
        {
            var rechargeWindow = new RechargeBalanceWindow(Email);
            if (rechargeWindow.ShowDialog() == true)
            {
                // Reload the profile data to update balance
                this.DataContext = new UserProfileViewModel(Email);
            }
        }

        private void ViewMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new MenuWindow(Email);
            menu.Show();
            this.Close();
        }

        private void CreateOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var order = new OrderWindow(Email);
            order.Show();
            this.Close();
        }

        private void OrderHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var orderHistory = new OrderHistoryWindow(Email);
            orderHistory.Show();
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
            if (this.DataContext is UserProfileViewModel vm)
            {
                await vm.SaveChangesAsync();
            }
        }

        private async void ChangePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is UserProfileViewModel vm)
            {
                await vm.ChangePhotoAsync();
            }
        }
    }
}
