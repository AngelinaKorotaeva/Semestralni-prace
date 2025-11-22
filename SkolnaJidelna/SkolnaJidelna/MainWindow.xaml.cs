using System.Windows;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.LoginFailed += msg => Dispatcher.Invoke(() => MessageBox.Show(this, msg, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error));
            vm.LoginSucceeded += (email, isPracovnik) => Dispatcher.Invoke(() =>
            {
                var profile = App.Services.GetService(typeof(UserProfileWindow)) as Window;
                if (profile != null)
                {
                    profile.Show();
                }
                else
                {
                    var p = new UserProfileWindow(email, isPracovnik);
                    p.Show();
                }
                this.Close();
            });
            vm.RegisterRequested += () => Dispatcher.Invoke(() =>
            {
                var reg = App.Services.GetService(typeof(LoginWindow)) as Window;
                if (reg == null)
                {
                    // fallback: vytvořit přes DI ViewModel
                    var vmReg = App.Services.GetService(typeof(RegisterViewModel)) as RegisterViewModel;
                    if (vmReg != null)
                    {
                        var wnd = new LoginWindow(vmReg);
                        wnd.Owner = this;
                        wnd.ShowDialog();
                    }
                }
                else
                {
                    reg.Owner = this;
                    reg.ShowDialog();
                }
            });
        }
    }
}