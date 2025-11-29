using System;
using System.Windows;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna
{
    /// <summary>
    /// Interakční logika pro AdminProfileWindow.xaml
    /// </summary>
    public partial class AdminProfileWindow : Window
    {
        public AdminProfileWindow()
        {
            InitializeComponent();
        }

        public AdminProfileWindow(string adminEmail) : this()
        {
            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                var vm = new AdminProfileViewModel(adminEmail);
                DataContext = vm;

                // DEBUG: potvrdit, že VM obsahuje jméno (okno se otevře a ukáže message)
                Loaded += (s, e) =>
                {
                    MessageBox.Show($"DataContext set: {DataContext != null}\nFullName: '{vm.FullName}'\nEmail: '{vm.Email}'",
                                    "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                };
            }
        }

        private void RechargeBalanceButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewMenuButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CreateOrderButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OrderHistoryButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AdminPanelButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
