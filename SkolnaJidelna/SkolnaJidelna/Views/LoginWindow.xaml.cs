//using BCrypt.Net; // Добавь NuGet-пакет BCrypt.Net-Next для хэширования паролей
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Media.Imaging;

namespace SkolniJidelna
{
    public partial class LoginWindow : Window
    {
        private byte[] selectedPhotoBytes;

        public LoginWindow()
        {
            InitializeComponent();
            LoadComboBoxes();
        }

        private async void LoadComboBoxes()
        {
            using (var context = new AppDbContext())
            {
                try
                {
                    // Загрузка позиций для работников
                    var positions = await context.Pozice.ToListAsync();
                    comboPosition.ItemsSource = positions;
                    comboPosition.DisplayMemberPath = "Nazev"; // Это названия позиций из базы (пока точно не знаю, как ты назовёшь этот атрибут ':) )

                    // Загрузка классов для студентов
                    var classes = await context.Trida.ToListAsync();
                    comboClass.ItemsSource = classes;
                    comboClass.DisplayMemberPath = "Cislo"; // Аналогично - номера классов из базы
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Chyba při načítání dat: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RadioStatus_Checked(object sender, RoutedEventArgs e)
        {
            if (radioWorker.IsChecked == true)
            {
                workerFields.Visibility = Visibility.Visible;
                studentFields.Visibility = Visibility.Collapsed;
            }
            else if (radioStudent.IsChecked == true)
            {
                workerFields.Visibility = Visibility.Collapsed;
                studentFields.Visibility = Visibility.Visible;
            }
        }

        private void BtnSelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Obrázky (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                Title = "Vyberte fotografii"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                selectedPhotoBytes = File.ReadAllBytes(filePath);

                // Отображение фото
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.EndInit();
                imagePhoto.Source = bitmap;
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация обязательных полей
            string firstName = textBoxFirstName.Text.Trim();
            string lastName = textBoxLastName.Text.Trim();
            string email = textBoxEmail.Text.Trim();
            string password = passwordBox.Password;
            string confirmPassword = passwordBoxConfirm.Password;

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vyplňte všechna povinná pole.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Hesla se neshodují.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (radioWorker.IsChecked != true && radioStudent.IsChecked != true)
            {
                MessageBox.Show("Vyberte status (Pracovník nebo Student).", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Проверка уникальности email
            using (var context = new AppDbContext())
            {
                try
                {
                    //bool emailExists = await context.Pracovnik.AnyAsync(p => p.Login == email) || await context.Stravnik.AnyAsync(s => s.Login == email);
                    //if (emailExists)
                    //{
                    //    MessageBox.Show("Tento email je již registrován.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //    return;
                    //}

                    // Хэширование пароля
                    //string hashedPassword = BCrypt.HashPassword(password);

                    if (radioWorker.IsChecked == true)
                    {
                        // Регистрация работника
                        var position = comboPosition.SelectedItem as Pozice;
                        string phone = textBoxPhone.Text.Trim();

                        if (position == null || string.IsNullOrEmpty(phone))
                        {
                            MessageBox.Show("Vyplňte pozici a telefon.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        //var worker = new Pracovnik
                        //{
                        //    Jmeno = firstName,
                        //    Prijmeni = lastName,
                        //    Login = email,
                        //    Heslo = hashedPassword,
                        //    Foto = selectedPhotoBytes,
                        //    PoziceId = position.Id, // Предполагаю поле Id в Pozice
                        //    Telefon = phone
                        //};

                        //context.Pracovnik.Add(worker);
                    }
                    else if (radioStudent.IsChecked == true)
                    {
                        // Регистрация студента
                        var classItem = comboClass.SelectedItem as Trida;

                        if (classItem == null)
                        {
                            MessageBox.Show("Vyberte třídu.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        //var student = new Stravnik
                        //{
                        //    Jmeno = firstName,
                        //    Prijmeni = lastName,
                        //    Login = email,
                        //    Heslo = hashedPassword,
                        //    Foto = selectedPhotoBytes,
                        //    TridaId = classItem.Id // Предполагаю поле Id в Trida
                        //};

                        //context.Stravnik.Add(student);
                    }

                    await context.SaveChangesAsync();
                    MessageBox.Show("Registrace úspěšná! Můžete se přihlásit.", "Registrace", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Возврат к логину
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Chyba při registraci: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Возврат на главную страницу
            var mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}
