using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using SkolniJidelna.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels;
public class AdminViewModel : INotifyPropertyChanged
{
    private readonly AppDbContext _db;

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public ObservableCollection<EntityTypeDescriptor> EntityTypes { get; } = new();
    public ObservableCollection<ItemViewModel> Items { get; } = new();
    public ObservableCollection<PropertyViewModel> Properties { get; } = new();

    private EntityTypeDescriptor? _selectedEntityType;
    public EntityTypeDescriptor? SelectedEntityType
    {
        get => _selectedEntityType;
        set
        {
            if (_selectedEntityType == value) return;
            _selectedEntityType = value;
            Raise(nameof(SelectedEntityType));
            _ = LoadItemsForSelectedEntityAsync();
        }
    }

    private ItemViewModel? _selectedItem;
    public ItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem == value) return;
            _selectedItem = value;
            Raise(nameof(SelectedItem));
            OnSelectedItemChanged(_selectedItem);
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CloseCommand { get; }

    public AdminViewModel(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        RefreshCommand = new RelayCommand(async _ => await LoadEntityTypesAsync());
        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => SelectedItem != null);
        CloseCommand = new RelayCommand(_ => { /* zavření okna řeší view */ });

        EntityTypes.Add(new EntityTypeDescriptor
        {
            Name = "Strávníci",
            EntityType = typeof(Models.Stravnik),
            LoaderAsync = async () =>
            {
                var list = await _db.Stravnik.AsNoTracking().ToListAsync();
                return list.Select(s => new ItemViewModel(s, $"{s.Jmeno} {s.Prijmeni} (ID {s.IdStravnik})")).ToList();
            }
        });

        EntityTypes.Add(new EntityTypeDescriptor
        {
            Name = "Objednávky",
            EntityType = typeof(Models.Objednavka),
            LoaderAsync = async () =>
            {
                var list = await _db.Objednavka.AsNoTracking().ToListAsync();
                return list.Select(o => new ItemViewModel(o, $"Objednávka {o.IdObjednavka} - {o.Datum:d}")).ToList();
            }
        });
    }

    public async Task LoadEntityTypesAsync()
    {
        if (EntityTypes.Count == 0) return;
        if (SelectedEntityType == null)
            SelectedEntityType = EntityTypes[0];
        await LoadItemsForSelectedEntityAsync();
    }

    public async Task LoadItemsForSelectedEntityAsync()
    {
        Items.Clear();
        Properties.Clear();
        SelectedItem = null;

        if (SelectedEntityType?.LoaderAsync == null) return;
        var items = await SelectedEntityType.LoaderAsync();
        foreach (var it in items) Items.Add(it);
    }

    public void OnSelectedItemChanged(ItemViewModel? item)
    {
        Properties.Clear();
        if (item == null) return;

        var props = item.Entity.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string))
            .ToArray();

        foreach (var p in props)
        {
            // připravíme počáteční hodnotu a callback, který nastaví hodnotu zpět na entitu
            var currentValue = p.GetValue(item.Entity);
            Action<object?> onChanged = val =>
            {
                try
                {
                    // jednoduchá konverze pokud je potřeba
                    if (val == null)
                    {
                        p.SetValue(item.Entity, null);
                    }
                    else
                    {
                        var targetType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                        var converted = Convert.ChangeType(val, targetType);
                        p.SetValue(item.Entity, converted);
                    }
                }
                catch
                {
                    
                }
            };

            Properties.Add(new PropertyViewModel(p.Name, p.PropertyType, currentValue, onChanged));
        }

        (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private async Task SaveAsync()
    {
        if (SelectedItem == null) return;
        try
        {
            _db.Update(SelectedItem.Entity);
            await _db.SaveChangesAsync();
            MessageBox.Show("Uloženo.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadItemsForSelectedEntityAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Chyba při ukládání: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}