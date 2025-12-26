using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SkolniJidelna.ViewModels;
// Základní ViewModel s implementací INotifyPropertyChanged
// Poskytuje metodu RaisePropertyChanged pro notifikaci UI o změnách vlastností
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    // Vyvolá událost PropertyChanged (název se doplní automaticky volajícím členem)
    protected void RaisePropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}