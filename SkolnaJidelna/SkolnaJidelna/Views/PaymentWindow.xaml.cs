using System.Windows;

namespace SkolniJidelna
{
    public partial class PaymentWindow : Window
    {
        public enum PaymentMethod
        {
            Card,
            Account,
            Cash
        }

        private readonly PaymentMethod _method;
        public PaymentWindow(PaymentMethod method)
        {
            InitializeComponent();
            _method = method;
            switch (_method)
            {
                case PaymentMethod.Card:
                    panelCard.Visibility = Visibility.Visible;
                    panelAccount.Visibility = Visibility.Collapsed;
                    panelCash.Visibility = Visibility.Collapsed;
                    break;
                case PaymentMethod.Account:
                    panelCard.Visibility = Visibility.Collapsed;
                    panelAccount.Visibility = Visibility.Visible;
                    panelCash.Visibility = Visibility.Collapsed;
                    break;
                case PaymentMethod.Cash:
                    panelCard.Visibility = Visibility.Collapsed;
                    panelAccount.Visibility = Visibility.Collapsed;
                    panelCash.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void PayButton_Click(object sender, RoutedEventArgs e)
        {
            // Here you would validate card fields if Card, and perform payment processing
            // For now, just close with DialogResult true
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}