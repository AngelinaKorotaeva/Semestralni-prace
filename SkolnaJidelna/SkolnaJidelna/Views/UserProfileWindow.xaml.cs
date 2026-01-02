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

            // If window.Tag contains original admin email, show a small Return button in titlebar
            this.Loaded += UserProfileWindow_Loaded;
        }

        private void UserProfileWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Tag is string originalAdmin && !string.IsNullOrWhiteSpace(originalAdmin))
            {
                // add a button for returning to admin profile in the window (simple MessageBox-driven fallback)
                var btn = new Button { Content = "Návrat do admina", Width =140, Height =28, Margin = new Thickness(10)};

                // Try to apply the LightButton style from resources
                try
                {
                    var lightStyle = this.TryFindResource("LightButton") as Style;
                    if (lightStyle == null && Application.Current != null && Application.Current.Resources.Contains("LightButton"))
                    {
                        lightStyle = Application.Current.Resources["LightButton"] as Style;
                    }
                    if (lightStyle != null) btn.Style = lightStyle;
                }
                catch
                {
                    // ignore resource lookup errors and keep default button style
                }

                btn.HorizontalAlignment = HorizontalAlignment.Right;
                btn.VerticalAlignment = VerticalAlignment.Top;
                btn.Click += (s, a) =>
                {
                    try
                    {
                        var apw = new AdminProfileWindow(originalAdmin);
                        apw.Show();
                        this.Close();
                    }
                    catch
                    {
                    }
                };

                // Add to main Grid root if exists
                if (this.Content is FrameworkElement fe && fe is Panel panel)
                {
                    panel.Children.Add(btn);
                }
            }
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
