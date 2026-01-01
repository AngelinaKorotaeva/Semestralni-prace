using System.Windows;

namespace SkolniJidelna
{
    public partial class CreateStravnikDialog : Window
    {
        public string Jmeno => txtJmeno.Text;
        public string Prijmeni => txtPrijmeni.Text;
        public string Email => txtEmail.Text;

        public CreateStravnikDialog()
        {
            InitializeComponent();
            btnOk.Click += BtnOk_Click;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
