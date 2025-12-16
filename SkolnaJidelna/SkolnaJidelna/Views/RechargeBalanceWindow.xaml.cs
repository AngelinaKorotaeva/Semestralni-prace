using System;
using System.Windows;
using SkolniJidelna.Data;
using SkolniJidelna.Models;

namespace SkolniJidelna
{
    public partial class RechargeBalanceWindow : Window
    {
        private string _email;

        public RechargeBalanceWindow(string email)
        {
            InitializeComponent();
            _email = email;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(AmountTextBox.Text, out var amount) && amount > 0)
            {
                try
                {
                    using var ctx = new AppDbContext();
                    var user = ctx.Stravnik.FirstOrDefault(s => s.Email == _email);
                    if (user != null)
                    {
                        user.Zustatek += (double)amount;
                        ctx.Platba.Add(new Platba
                        {
                            Datum = DateTime.Now,
                            Castka = (double)amount,
                            Metoda = "UI Doplnìní",
                            IdStravnik = user.IdStravnik
                        });
                        ctx.SaveChanges();
                        MessageBox.Show("Zustatek byl uspesne doplnen.", "Uspech", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                    }
                    else
                    {
                        MessageBox.Show("Uzivatel nenalezen.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Chyba pri doplneni zustatku: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Zadejte platnou castku vetsi nez 0.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}