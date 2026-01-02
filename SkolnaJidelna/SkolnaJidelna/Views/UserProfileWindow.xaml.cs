using System;
using System.Linq;
using System.Windows;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using Microsoft.Extensions.DependencyInjection;
using SkolniJidelna.ViewModels;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

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
                // , Margin = new Thickness(5)
                var btn = new Button { Content = "Návrat do admina", Width =200, Height =50};

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

            LoadAllergiesAndDietRestrictions();
        }

        private void LoadAllergiesAndDietRestrictions()
        {
            try
            {
                using var ctx = new AppDbContext();
                // Try resolve email from DataContext if not provided
                var email = Email;
                if (string.IsNullOrWhiteSpace(email) && DataContext is BaseViewModel vm)
                {
                    var prop = vm.GetType().GetProperty("Email");
                    if (prop != null)
                    {
                        email = prop.GetValue(vm)?.ToString();
                    }
                }

                // Pohled V_STR_OMEZENI_ALERGIE
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var row = ctx.VStravnikOmezeniAlergie
                        .AsNoTracking()
                        .FirstOrDefault(v => v.Email == email);

                    if (row != null)
                    {
                        var alergie = string.IsNullOrWhiteSpace(row.Alergie) ? "-" : row.Alergie;
                        var omezeni = string.IsNullOrWhiteSpace(row.DietniOmezeni) ? "-" : row.DietniOmezeni;
                        var text = $"Alergie: {alergie} | Dietní omezení: {omezeni}";
                        // Assign to a TextBlock named ProfileAlergieText if present
                        var tb = this.FindName("ProfileAlergieText") as System.Windows.Controls.TextBlock;
                        if (tb != null) tb.Text = text;
                    }
                }
            }
            catch (Exception)
            {
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
