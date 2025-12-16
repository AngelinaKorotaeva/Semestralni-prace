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
using System.ComponentModel.DataAnnotations.Schema;

namespace SkolniJidelna.ViewModels;
public class AdminViewModel : INotifyPropertyChanged
{
    private readonly AppDbContext _db;

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public ObservableCollection<EntityTypeDescriptor> EntityTypes { get; } = new();
    public ObservableCollection<ItemViewModel> Items { get; } = new();
    public ObservableCollection<PropertyViewModel> Properties { get; } = new();
    public ObservableCollection<string> TableProperties { get; } = new();

    private EntityTypeDescriptor? _selectedEntityType;
    public EntityTypeDescriptor? SelectedEntityType
    {
        get => _selectedEntityType;
        set
        {
            if (_selectedEntityType == value) return;
            _selectedEntityType = value;
            Raise(nameof(SelectedEntityType));
            TableProperties.Clear();
            if (_selectedEntityType != null)
            {
                var props = _selectedEntityType.EntityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && p.GetCustomAttribute<ColumnAttribute>() != null)
                    .Select(p => $"{p.Name}");
                foreach (var p in props) TableProperties.Add(p);
            }
            // Load items automatically when entity type changes
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
    public ICommand DeleteCommand { get; }

    public AdminViewModel(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        RefreshCommand = new RelayCommand(async _ => await LoadEntityTypesAsync());
        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => SelectedItem != null);
        CloseCommand = new RelayCommand(_ => { /* zavření okna řeší view */ });
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedItem != null);

        // Dynamically load all entity types from DbContext
        var dbSetProperties = typeof(AppDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToArray();

        foreach (var prop in dbSetProperties)
        {
            var entityType = prop.PropertyType.GetGenericArguments()[0];
            var name = entityType.Name; // or use a custom display name

            // Skip certain tables
            if (name == "StravnikAlergie" || name == "StravnikOmezeni" || name == "VStravnikLogin" || name == "Log")
                continue;

            EntityTypes.Add(new EntityTypeDescriptor
            {
                Name = name,
                EntityType = entityType,
                LoaderAsync = async () =>
                {
                    try
                    {
                        MessageBox.Show($"Starting load for {entityType.Name}");
                        // Get DbSet using Set<T>
                        var setMethod = typeof(DbContext).GetMethods()
                            .Where(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 0)
                            .FirstOrDefault()?.MakeGenericMethod(entityType);
                        var dbSet = setMethod?.Invoke(_db, null) as IQueryable;
                        if (dbSet == null) return new List<ItemViewModel>();

                        // Call ToListAsync using reflection
                        var toListAsyncMethod = typeof(Queryable).GetMethods()
                            .Where(m => m.Name == "ToList" && m.GetParameters().Length == 1 && m.GetGenericArguments().Length == 1)
                            .FirstOrDefault()?.MakeGenericMethod(entityType);
                        if (toListAsyncMethod == null) return new List<ItemViewModel>();

                        // Limit to 100 items to avoid hanging
                        var takeMethod = typeof(Queryable).GetMethods()
                            .Where(m => m.Name == "Take" && m.GetParameters().Length == 2 && m.GetGenericArguments().Length == 1)
                            .FirstOrDefault()?.MakeGenericMethod(entityType);
                        if (takeMethod != null)
                        {
                            dbSet = takeMethod.Invoke(null, new object[] { dbSet, 100 }) as IQueryable;
                        }

                        var task = (Task)toListAsyncMethod.Invoke(null, new[] { dbSet });
                        await task;
                        var resultProperty = task.GetType().GetProperty("Result");
                        var list = resultProperty?.GetValue(task) as System.Collections.IList;
                        MessageBox.Show($"List count for {entityType.Name}: {list?.Count ?? 0}");
                        if (list == null) return new List<ItemViewModel>();

                        var items = new List<ItemViewModel>();
                        foreach (var item in list)
                        {
                            var summary = $"{entityType.Name} {GetIdValue(item)}";
                            items.Add(new ItemViewModel(item, summary));
                        }
                        return items;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading {entityType.Name}: {ex.Message}");
                        return new List<ItemViewModel>();
                    }
                }
            });
        }
    }

    public async Task LoadEntityTypesAsync()
    {
        if (EntityTypes.Count == 0) return;
        if (SelectedEntityType == null)
            SelectedEntityType = EntityTypes.FirstOrDefault(e => e.Name == "Jidlo") ?? EntityTypes[0];
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

    private async Task DeleteAsync()
    {
        if (SelectedItem == null) return;
        try
        {
            _db.Remove(SelectedItem.Entity);
            await _db.SaveChangesAsync();
            MessageBox.Show("Smazáno.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadItemsForSelectedEntityAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Chyba při mazání: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string GetIdValue(object entity)
    {
        var type = entity.GetType();
        var idProp = type.GetProperty("Id" + type.Name) ?? type.GetProperties().FirstOrDefault(p => p.Name.EndsWith("Id") || p.Name.StartsWith("Id"));
        if (idProp != null)
        {
            var value = idProp.GetValue(entity);
            return value?.ToString() ?? "N/A";
        }
        return "N/A";
    }
}