using System.Windows;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna;
public partial class LoginWindow : Window
{
    public LoginWindow(RegisterViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        // Zobrazení zpráv a zavření okna volá ViewModel
        vm.RequestMessage += msg => Dispatcher.Invoke(() => MessageBox.Show(this, msg, "Informace", MessageBoxButton.OK, MessageBoxImage.Information));
        vm.RequestClose += () => Dispatcher.Invoke(() => this.Close());
    }
}