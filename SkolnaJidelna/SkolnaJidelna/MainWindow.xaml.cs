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

            // WindowService already shows the correct profile window.
            // Do not create windows here to avoid opening two windows.
            vm.LoginSucceeded += (email, isPracovnik, isAdmin) => Dispatcher.Invoke(() =>
            {
                // Navigation handled by IWindowService from the ViewModel.
                // Just hide the login window so the opened profile window is visible.
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