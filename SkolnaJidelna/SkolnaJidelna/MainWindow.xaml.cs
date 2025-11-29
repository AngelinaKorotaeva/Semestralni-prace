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
        if (DataContext is MainWindowViewModel vm)
        {
            vm.LoginFailed += msg => Dispatcher.Invoke(() =>
                MessageBox.Show(this, msg, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error));

            vm.LoginSucceeded += (email, isPracovnik, isAdmin) => Dispatcher.Invoke(() =>
            {
                if (isAdmin)
                {
                    var wnd = new AdminProfileWindow(email);
                    wnd.Show();
                }
                else
                {
                    var wnd = new UserProfileWindow(email);
                    wnd.Show();
                }

                this.Hide();
            });

            vm.RegisterRequested += () => Dispatcher.Invoke(() =>
            {
                this.Hide();
                try
                {
                    var reg = App.Services.GetService(typeof(LoginWindow)) as Window;
                    if (reg == null)
                    {
                        var vmReg = App.Services.GetService(typeof(RegisterViewModel)) as RegisterViewModel;
                        if (vmReg != null)
                        {
                            var wnd = new LoginWindow(vmReg); // registrační okno používá jako dialog v tomto projektu
                            wnd.Owner = this;
                            wnd.ShowDialog();
                        }
                    }
                    else
                    {
                        reg.Owner = this;
                        reg.ShowDialog();
                    }
                }
                finally
                {
                    this.Show();
                }
            });
        }
    }
}