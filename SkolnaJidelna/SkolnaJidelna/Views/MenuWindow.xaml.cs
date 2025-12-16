using System;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna
{
    public partial class MenuWindow : Window
    {
        private string Email;
        private MenuViewModel _vm;

        public MenuWindow(string email)
        {
            InitializeComponent();
            Email = email;
            _vm = new MenuViewModel();
            this.DataContext = _vm;
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            // Read selected value from ComboBox and set on VM
            var selected = (comboTypMenu.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrWhiteSpace(selected))
            {
                _vm.SelectedTypMenu = selected;
            }
            else
            {
                _vm.LoadMenus();
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            bool isAdmin = false;
            bool isPracovnik = false;
            try
            {
                using var ctx = new Data.AppDbContext();
                var v = ctx.VStravnikLogin.AsNoTracking().FirstOrDefault(x => x.Email == Email);
                var role = v?.Role?.Trim();
                var type = v?.TypStravnik?.Trim();
                isAdmin = string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);
                isPracovnik = string.Equals(type, "pr", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
            }

            try
            {
                var svc = App.Services.GetService(typeof(SkolniJidelna.Services.IWindowService)) as SkolniJidelna.Services.IWindowService;
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
    }
}
