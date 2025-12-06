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
using SkolniJidelna.Services;

namespace SkolniJidelna
{
    /// <summary>
    /// Interakční logika pro OrderWindow.xaml
    /// </summary>
    public partial class OrderWindow : Window
    {
        private string Email;
        public OrderWindow(string email)
        {
            InitializeComponent();

            this.Email = email;
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void comboTypMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void textBoxPoznamka_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
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
            catch
            {
                // ignore DB errors and fall back to non-admin behavior
            }

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
                    // fallback: create window directly
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

        private void AddToOrderButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RemoveFromOrderButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
