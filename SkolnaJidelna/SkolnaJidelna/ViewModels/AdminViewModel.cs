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
    public ObservableCollection<string> Classes { get; } = new();
    public ObservableCollection<string> Positions { get; } = new();
    public ObservableCollection<string> FoodCategories { get; } = new();
    public ObservableCollection<string> DietTypes { get; } = new();
    private ObservableCollection<string> _currentList = new();
    public ObservableCollection<string> CurrentList
    {
        get => _currentList;
        set
        {
            if (_currentList == value) return;
            _currentList = value;
            Raise(nameof(CurrentList));
        }
    }

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
            Classes.Clear();
            Positions.Clear();
            FoodCategories.Clear();
            SelectedClass = null;
            SelectedPosition = null;
            SelectedFoodCategory = null;
            SelectedDietType = null;
            if (_selectedEntityType != null)
            {
                if (_selectedEntityType.Name == "Studenti")
                {
                    // Load classes asynchronously
                    _ = LoadClassesAsync();
                    CurrentList = Classes;
                }
                else if (_selectedEntityType.Name == "Pracovníci")
                {
                    // Load positions asynchronously
                    _ = LoadPositionsAsync();
                    CurrentList = Positions;
                }
                else if (_selectedEntityType.Name == "Jídla")
                {
                    // Load food categories asynchronously
                    _ = LoadFoodCategoriesAsync();
                    CurrentList = FoodCategories;
                }
                else if (_selectedEntityType.Name == "Dietní omezení")
                {
                    // Load diet types
                    DietTypes.Clear();
                    DietTypes.Add("Vše");
                    DietTypes.Add("Alergie");
                    DietTypes.Add("Dietní omezení");
                    CurrentList = DietTypes;
                }
                else
                {
                    var props = _selectedEntityType.EntityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanRead && p.GetCustomAttribute<ColumnAttribute>() != null)
                        .Select(p => $"{p.Name}");
                    TableProperties.Clear();
                    foreach (var p in props) TableProperties.Add(p);
                    CurrentList = TableProperties;
                }
            }
            else
            {
                CurrentList = TableProperties;
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

    private string? _selectedClass;
    public string? SelectedClass
    {
        get => _selectedClass;
        set
        {
            if (_selectedClass == value) return;
            _selectedClass = value;
            Raise(nameof(SelectedClass));
            _ = LoadItemsForSelectedEntityAsync();
        }
    }

    private string? _selectedPosition;
    public string? SelectedPosition
    {
        get => _selectedPosition;
        set
        {
            if (_selectedPosition == value) return;
            _selectedPosition = value;
            Raise(nameof(SelectedPosition));
            _ = LoadItemsForSelectedEntityAsync();
        }
    }

    private string? _selectedFoodCategory;
    public string? SelectedFoodCategory
    {
        get => _selectedFoodCategory;
        set
        {
            if (_selectedFoodCategory == value) return;
            _selectedFoodCategory = value;
            Raise(nameof(SelectedFoodCategory));
            _ = LoadItemsForSelectedEntityAsync();
        }
    }

    private string? _selectedDietType;
    public string? SelectedDietType
    {
        get => _selectedDietType;
        set
        {
            if (_selectedDietType == value) return;
            _selectedDietType = value;
            Raise(nameof(SelectedDietType));
            _ = LoadItemsForSelectedEntityAsync();
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

        // Statically load entity types
        var entityTypesToInclude = new[]
        {
            ("Studenti", typeof(Student)),
            ("Pracovníci", typeof(Pracovnik)),
            ("Jídla", typeof(Jidlo)),
            ("Dietní omezení", typeof(DietniOmezeni))
        };

        foreach (var (displayName, entityType) in entityTypesToInclude)
        {
            EntityTypes.Add(new EntityTypeDescriptor
            {
                Name = displayName,
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

                        if (entityType == typeof(Student))
                        {
                            // Use VStudTrida for display
                            var query = _db.VStudTrida.AsQueryable();
                            if (!string.IsNullOrEmpty(_selectedClass) && _selectedClass != "Vše")
                            {
                                if (int.TryParse(_selectedClass, out int cislo))
                                {
                                    query = query.Where(s => s.CisloTridy == cislo);
                                }
                            }
                            var students = await query.Take(100).ToListAsync();
                            var studentItems = new List<ItemViewModel>();
                            foreach (var stud in students)
                            {
                                var summary = $"{stud.Jmeno} - {stud.Email} - {stud.DatumNarozeni.ToShortDateString()} - Alergie: {stud.Alergie ?? "Žádné"} - Omezení: {stud.Omezeni ?? "Žádné"}";
                                studentItems.Add(new ItemViewModel(stud, summary));
                            }
                            return studentItems;
                        }
                        else if (entityType == typeof(Pracovnik))
                        {
                            // Use VPracovnikPozice for display
                            var query = _db.VPracovnikPozice.AsQueryable();
                            if (!string.IsNullOrEmpty(_selectedPosition) && _selectedPosition != "Vše")
                            {
                                query = query.Where(p => p.Pozice == _selectedPosition);
                            }
                            var workers = await query.Take(100).ToListAsync();
                            var workerItems = new List<ItemViewModel>();
                            foreach (var worker in workers)
                            {
                                var summary = $"{worker.Jmeno} - {worker.Email} - Telefon: {worker.Telefon} - Pozice: {worker.Pozice} - Alergie: {worker.Alergie ?? "Žádné"} - Omezení: {worker.Omezeni ?? "Žádné"}";
                                workerItems.Add(new ItemViewModel(worker, summary));
                            }
                            return workerItems;
                        }
                        else if (entityType == typeof(Jidlo))
                        {
                            // Use VJidlaSlozeni for display
                            var query = _db.VJidlaSlozeni.AsQueryable();
                            if (!string.IsNullOrEmpty(_selectedFoodCategory) && _selectedFoodCategory != "Vše")
                            {
                                query = query.Where(j => j.Kategorie == _selectedFoodCategory);
                            }
                            var foods = await query.Take(100).ToListAsync();
                            var foodItems = new List<ItemViewModel>();
                            foreach (var food in foods)
                            {
                                var summary = $"{food.NazevJidla} - {food.PopisJidla} - Cena: {food.Cena} - Složení: {food.Slozeni}";
                                foodItems.Add(new ItemViewModel(food, summary));
                            }
                            return foodItems;
                        }
                        else if (entityType == typeof(DietniOmezeni))
                        {
                            var dietItems = new List<ItemViewModel>();
                            if (_selectedDietType == "Alergie" || _selectedDietType == "Vše")
                            {
                                var alergies = await _db.Alergie.Take(100).ToListAsync();
                                foreach (var a in alergies)
                                {
                                    var summary = $"Alergie: {a.Nazev}";
                                    dietItems.Add(new ItemViewModel(a, summary));
                                }
                            }
                            if (_selectedDietType == "Dietní omezení" || _selectedDietType == "Vše")
                            {
                                var omezeni = await _db.DietniOmezeni.Take(100).ToListAsync();
                                foreach (var o in omezeni)
                                {
                                    var summary = $"Dietní omezení: {o.Nazev}";
                                    dietItems.Add(new ItemViewModel(o, summary));
                                }
                            }
                            return dietItems;
                        }

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
                            var summary = $"{displayName} {GetIdValue(item)}";
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
        // Do not set SelectedEntityType by default
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

    public async Task SaveAsync()
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

    public async Task LoadClassesAsync()
    {
        var classes = await _db.Trida.Select(t => t.CisloTridy).ToListAsync();
        Classes.Clear();
        Classes.Add("Vše");
        foreach (var c in classes.OrderBy(c => c)) Classes.Add(c.ToString());
    }

    public async Task LoadPositionsAsync()
    {
        var positions = await _db.Pozice.Select(p => p.Nazev).ToListAsync();
        Positions.Clear();
        Positions.Add("Vše");
        foreach (var p in positions.OrderBy(p => p)) Positions.Add(p);
    }

    public async Task LoadFoodCategoriesAsync()
    {
        var foodCategories = await _db.Jidlo.Select(j => j.Kategorie).Distinct().ToListAsync();
        FoodCategories.Clear();
        FoodCategories.Add("Vše");
        foreach (var c in foodCategories.OrderBy(c => c)) FoodCategories.Add(c);
    }

    public async Task LoadDietTypesAsync()
    {
        var dietTypes = await _db.DietniOmezeni.Select(d => d.Nazev).Distinct().ToListAsync();
        DietTypes.Clear();
        DietTypes.Add("Vše");
        foreach (var d in dietTypes.OrderBy(d => d)) DietTypes.Add(d);
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