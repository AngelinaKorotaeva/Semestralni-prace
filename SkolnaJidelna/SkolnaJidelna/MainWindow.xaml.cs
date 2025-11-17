//using BCrypt.Net; // Надо добавить NuGet-пакет BCrypt.Net-Next для проверки хэшированных паролей (?)
using System;
using System.Linq;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using static System.Net.Mime.MediaTypeNames;

namespace SkolniJidelna
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextBoxLogin_GotFocus(object sender, RoutedEventArgs e)
        {
            if (textBoxLogin.Text == "Uživatelské jméno")
            {
                textBoxLogin.Text = "";
                textBoxLogin.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void textBoxLogin_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Проверка на пустоту или формат логина - должен быть и-мэйл
            string text = textBoxLogin.Text.Trim();
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (string.IsNullOrWhiteSpace(textBoxLogin.Text) || textBoxLogin.Text == "Uživatelské jméno")
            {
                textBoxLogin.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            else if (!Regex.IsMatch(text, emailPattern))
            {
                textBoxLogin.BorderBrush = System.Windows.Media.Brushes.Red;
            }
            else
            {
                textBoxLogin.BorderBrush = System.Windows.Media.Brushes.Gray; // Сброс
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e) // async
        {
            string username = textBoxLogin.Text.Trim();
            string password = passwordBox.Password;

            if (string.IsNullOrEmpty(username) || username == "Uživatelské jméno" || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Prosím vyplňte všechny údaje", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var context = new AppDbContext())
            {
                try
                {
                    // 🔹 Вход — pracovník
                    var stravnikPR = await context.Stravnik
                        .FirstOrDefaultAsync(s => s.Email == username);

                    if (stravnikPR != null && BCrypt.Net.BCrypt.Verify(password, stravnikPR.Heslo))
                    {
                        MessageBox.Show($"Úspěšně přihlášen jako pracovník: {stravnikPR.Jmeno} {stravnikPR.Prijmeni}",
                                        "Přihlášení", MessageBoxButton.OK, MessageBoxImage.Information);

                        var profileWindow = new UserProfileWindow(stravnikPR.Email, true);
                        profileWindow.Show();
                        this.Close();
                        return;
                    }

                    // 🔹 Вход — student
                    var stravnikST = await context.Stravnik
                        .FirstOrDefaultAsync(s => s.Email == username);

                    if (stravnikST != null && BCrypt.Net.BCrypt.Verify(password, stravnikST.Heslo))
                    {
                        MessageBox.Show($"Úspěšně přihlášen jako student: {stravnikST.Jmeno} {stravnikST.Prijmeni}",
                                        "Přihlášení", MessageBoxButton.OK, MessageBoxImage.Information);

                        var profileWindow = new UserProfileWindow(stravnikST.Email, false);
                        profileWindow.Show();
                        this.Close();
                        return;
                    }

                    // 🔹 Неверный логин/пароль
                    MessageBox.Show("Nesprávné uživatelské jméno nebo heslo",
                                    "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Chyba při přihlašování: {ex.Message}",
                                    "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
