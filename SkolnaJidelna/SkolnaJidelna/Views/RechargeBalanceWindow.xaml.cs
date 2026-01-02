using System;
using System.Linq;
using System.Windows;
using SkolniJidelna.Data;
using SkolniJidelna.Models;

namespace SkolniJidelna
{
    public partial class RechargeBalanceWindow : Window
    {
        private readonly string _email;

        public RechargeBalanceWindow(string email)
        {
            InitializeComponent();
            _email = email ?? string.Empty;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(AmountTextBox.Text, out var amountDec) && amountDec > 0)
            {
                try
                {
                    using var ctx = new AppDbContext();
                    var user = ctx.Stravnik.FirstOrDefault(s => s.Email == _email);
                    if (user != null)
                    {
                        var amount = (double)amountDec;

                        // Update balance
                        user.Zustatek += amount;

                        // Add payment record (Czech text fixed)
                        ctx.Platba.Add(new Platba
                        {
                            Datum = DateTime.Now,
                            Castka = amount,
                            Metoda = "UI Doplnění",
                            IdStravnik = user.IdStravnik
                        });

                        ctx.SaveChanges();
                        
                        // Open PaymentWindow after confirming amount
                        var payWin = new PaymentWindow(PaymentWindow.PaymentMethod.Card) { Owner = this.Owner };
                        var result = payWin.ShowDialog();
                        if (result == true)
                        {
                            this.DialogResult = true;
                        }
                    }
                    else
                    {
                        MessageBox.Show("Uživatel nenalezen.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Chyba při doplnění zůstatku: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Zadejte platnou částku větší než 0.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}