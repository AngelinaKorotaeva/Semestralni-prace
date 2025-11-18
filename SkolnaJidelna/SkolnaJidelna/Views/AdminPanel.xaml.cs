using System.Windows;
using SkolniJidelna.ViewModels;

namespace SkolniJidelna;
public partial class AdminPanel : Window
{
    private readonly AdminViewModel _vm;
    public AdminPanel(AdminViewModel vm)
    {
        InitializeComponent();
        _vm = vm ?? throw new System.ArgumentNullException(nameof(vm));
        DataContext = _vm;
        _vm.CloseRequested += () => Dispatcher.Invoke(Close);
        Loaded += async (_, _) => { await _vm.InitializeAsync(); };
    }
}