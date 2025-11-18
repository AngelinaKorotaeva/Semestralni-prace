using System.Collections.ObjectModel;
using System.Windows.Input;
using SkolniJidelna.Models;
using SkolniJidelna.Services;
using System.Threading.Tasks;

namespace SkolniJidelna.ViewModels;
public class StravnikListViewModel : BaseViewModel
{
    private readonly IStravnikRepository _repo;
    public ObservableCollection<Stravnik> Stravnici { get; } = new();
    public ICommand RefreshCommand { get; }

    public StravnikListViewModel(IStravnikRepository repo)
    {
        _repo = repo;
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        var list = await _repo.GetAllAsync();
        Stravnici.Clear();
        foreach (var s in list) Stravnici.Add(s);
    }
}