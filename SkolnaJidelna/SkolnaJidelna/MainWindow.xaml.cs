//using BCrypt.Net; // Надо добавить NuGet-пакет BCrypt.Net-Next для проверки хэшированных паролей (?)
using Microsoft.EntityFrameworkCore;
using SkolnaJidelna.Data;
using SkolnaJidelna.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
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

        private  void LoginButton_Click(object sender, RoutedEventArgs e) // async
        {
            //string username = textBoxLogin.Text.Trim();
            //string password = passwordBox.Password;

            //if (string.IsNullOrEmpty(username) || username == "Uživatelské jméno" || string.IsNullOrEmpty(password))
            //{
            //    MessageBox.Show("Prosím vyplňte všechny údaje", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    return;
            //}

            //using (var context = new AppDbContext())
            //{
            //    try
            //    {
                    // Проверка для работников
                    //var worker = await context.Pracovnik.FirstOrDefaultAsync(p => p.Login == username);
                    //if (worker != null && BCrypt.Verify(password, worker.Heslo))
                    //{
                    //    // Успешный вход для работника — открываем профиль
                    //    MessageBox.Show($"Úspěšně přihlášen jako pracovník: {worker.Jmeno} {worker.Prijmeni}", "Přihlašení", MessageBoxButton.OK, MessageBoxImage.Information);
                    //    var profileWindow = new UserProfileWindow(worker.Id, true); // true для работника
                    //    profileWindow.Show();
                    //    this.Close();
                    //    return;
                    //}

                    // Проверка для студентов
                    //var student = await context.Stravnik.FirstOrDefaultAsync(s => s.Login == username);
                    //if (student != null && BCrypt.Verify(password, student.Heslo))
                    //{
                    //    // Успешный вход для студента — схема та же :)
                    //    MessageBox.Show($"Úspěšně přihlášen jako student: {student.Jmeno} {student.Prijmeni}", "Přihlašení", MessageBoxButton.OK, MessageBoxImage.Information);
                    //    var profileWindow = new UserProfileWindow(worker.Id, false);
                    //    profileWindow.Show();
                    //    this.Close();
                    //    return;
                    //}

                    // Если не найдено
            //        MessageBox.Show("Nesprávné uživatelské jméno nebo heslo", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //    catch (Exception ex)
            //    {
            //        MessageBox.Show($"Chyba při přihlašování: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //}
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Открываем окно регистрации
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close(); 
        }
    }
}
