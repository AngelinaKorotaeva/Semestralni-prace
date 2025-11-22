using System.Windows;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna;
public partial class LoginWindow : Window
{
    // This window is the registration dialog — accepts RegisterViewModel only
    public LoginWindow(RegisterViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        vm.RequestMessage += msg => Dispatcher.Invoke(() => MessageBox.Show(this, msg, "Informace", MessageBoxButton.OK, MessageBoxImage.Information));
        vm.RequestClose += () => Dispatcher.Invoke(() => this.Close());
    }
}