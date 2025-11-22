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
            // Показ ошибок в окне
            vm.LoginFailed += msg => Dispatcher.Invoke(() =>
                MessageBox.Show(this, msg, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error));

            // На успешный вход VM навигирует через IWindowService; view просто закрывается/скрывается
            vm.LoginSucceeded += (email, isPracovnik, isAdmin) => Dispatcher.Invoke(() =>
            {
                // VM уже открыл соответствующее окно через сервис, здесь просто закрываем главное (login) окно
                this.Close();
            });

            vm.RegisterRequested += () => Dispatcher.Invoke(() =>
            {
                // показать окно registrace (логика уже была в приложении; оставляем существующее поведение)
                this.Hide();
                try
                {
                    var reg = App.Services.GetService(typeof(LoginWindow)) as Window;
                    if (reg == null)
                    {
                        var vmReg = App.Services.GetService(typeof(RegisterViewModel)) as RegisterViewModel;
                        if (vmReg != null)
                        {
                            var wnd = new LoginWindow(vmReg); // регистрационное окно используется как диалог в этом проекте
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