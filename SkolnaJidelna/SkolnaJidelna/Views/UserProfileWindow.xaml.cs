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
            

            LoadUserData(Email);
        }

        private async void LoadUserData(String email)
        {
            using var db = new AppDbContext();

            var stravnik = await db.Stravnik
                .Include(s => s.Alergie)
                .Include(s => s.Omezeni)
                .Include(s => s.Platby)
                .FirstOrDefaultAsync(s => s.Email == email);

            if (stravnik == null)
            {
                MessageBox.Show("Uživatel nebyl nalezen.");
                Close();
                return;
            }

            textBalance.Text = $"{stravnik.Zustatek} Kč";

            textName.Text = $"{stravnik.Jmeno} {stravnik.Prijmeni}";

            textStatus.Text = stravnik.TypStravnik == "student" ? "Status: Student" : "Status: Pracovník";


            if (stravnik.TypStravnik == "student")
            {
                var student = await db.Student
                    .Include(t => t.Trida)
                    .FirstOrDefaultAsync(s => s.IdStravnik == stravnik.IdStravnik);

                textPositionClass.Text = student != null ? $"Třída: {student.Trida.CisloTridy}" : "-";

                comboAlergies.Visibility = Visibility.Collapsed;
                comboDietRestrictions.Visibility = Visibility.Collapsed;
                saveButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                var pracovnik = await db.Pracovnik
                    .Include(p => p.Pozice)
                    .FirstOrDefaultAsync(s => s.IdStravnik == stravnik.IdStravnik);

                textPositionClass.Text = pracovnik != null ? $"Pozice: {pracovnik.Pozice.Nazev}" : "-";

                comboAlergies.Visibility = Visibility.Visible;
                comboDietRestrictions.Visibility = Visibility.Visible;
                saveButton.Visibility = Visibility.Visible;
                textAlergies.Visibility = Visibility.Collapsed;
                textDietRestrictions.Visibility = Visibility.Collapsed;
            }
        }

        private void RechargeBalanceButton_Click(object sender, RoutedEventArgs e)
        {
            //var balance = new PaymentWindow();
            //balance.Show();
            //this.Close();
        }

        private void ViewMenuButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new MenuWindow();
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
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
        }
            
    }
}
