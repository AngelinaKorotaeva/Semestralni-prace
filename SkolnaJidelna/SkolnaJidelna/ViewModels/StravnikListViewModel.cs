using System.Collections.ObjectModel;
using System.Windows.Input;
using SkolniJidelna.Models;
using SkolniJidelna.Services;
using System.Threading.Tasks;
using SkolniJidelna.Helpers;

namespace SkolniJidelna.ViewModels;
// ViewModel seznamu strávníků – načítá data z repozitáře a poskytuje Refresh příkaz
public class StravnikListViewModel : BaseViewModel
{
    private readonly IStravnikRepository _repo;
    public ObservableCollection<Stravnik> Stravnici { get; } = new(); // Kolekce pro binding do UI
    public ICommand RefreshCommand { get; }

    public StravnikListViewModel(IStravnikRepository repo)
    {
        _repo = repo;
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
    }

    // Načte strávníky z repozitáře a obnoví kolekci
    public async Task LoadAsync()
    {
        var list = await _repo.GetAllAsync();
        Stravnici.Clear();
        foreach (var s in list) Stravnici.Add(s);
    }
}