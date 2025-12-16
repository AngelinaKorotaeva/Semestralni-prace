using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;
using SkolniJidelna.Services;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna
{
    public partial class OrderWindow : Window
    {
        private string Email;
        private OrderViewModel _vm;
        public OrderWindow(string email)
        {
            InitializeComponent();
            this.Email = email;
            _vm = new OrderViewModel();
            this.DataContext = _vm;
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = (comboTypMenu.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                _vm.SelectedTypMenu = selected;
            }
            _vm.LoadMenus();
        }

        private void comboTypMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void textBoxPoznamka_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            PaymentWindow.PaymentMethod method = PaymentWindow.PaymentMethod.Card;
            if (radioPlatbaUctem.IsChecked == true) method = PaymentWindow.PaymentMethod.Account;
            else if (radioPlatbaPriVyzvednuti.IsChecked == true) method = PaymentWindow.PaymentMethod.Cash;

            var pw = new PaymentWindow(method) { Owner = this };
            var ok = pw.ShowDialog();
            if (ok == true)
            {
                try
                {
                    _vm.CreateOrder(Email, MapMethod(method), textBoxPoznamka.Text);
                    MessageBox.Show("Objednávka byla vytvořena.", "Úspěch", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Navigate back to profile depending on role
                    NavigateBackToProfile();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Chyba při vytvoření objednávky: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void NavigateBackToProfile()
        {
            bool isAdmin = false;
            bool isPracovnik = false;
            try
            {
                using var ctx = new AppDbContext();
                var v = ctx.VStravnikLogin.AsNoTracking().FirstOrDefault(x => x.Email == Email);
                var role = v?.Role?.Trim();
                var type = v?.TypStravnik?.Trim();
                isAdmin = string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);
                isPracovnik = string.Equals(type, "pr", StringComparison.OrdinalIgnoreCase);
            }
            catch { }

            try
            {
                var svc = App.Services.GetService(typeof(IWindowService)) as IWindowService;
                if (svc != null)
                {
                    if (isAdmin)
                        svc.ShowAdminProfile(Email);
                    else
                        svc.ShowUserProfile(Email, isPracovnik);
                }
                else
                {
                    if (isAdmin)
                    {
                        var aw = new AdminProfileWindow(Email);
                        aw.Show();
                    }
                    else
                    {
                        var up = new UserProfileWindow(Email);
                        up.Show();
                    }
                }
            }
            catch
            {
                if (isAdmin)
                {
                    var aw = new AdminProfileWindow(Email);
                    aw.Show();
                }
                else
                {
                    var up = new UserProfileWindow(Email);
                    up.Show();
                }
            }

            this.Close();
        }

        private OrderViewModel.PaymentMethod MapMethod(PaymentWindow.PaymentMethod m)
        {
            return m switch
            {
                PaymentWindow.PaymentMethod.Account => OrderViewModel.PaymentMethod.Account,
                PaymentWindow.PaymentMethod.Cash => OrderViewModel.PaymentMethod.Cash,
                _ => OrderViewModel.PaymentMethod.Card
            };
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateBackToProfile();
        }

        private void AddToOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.Tag as OrderViewModel.JidloItem;
            if (item != null)
            {
                _vm.AddToOrder(item);
            }
        }

        private void RemoveFromOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag is int id)
            {
                _vm.RemoveOneFromOrder(id);
            }
        }
    }
}
