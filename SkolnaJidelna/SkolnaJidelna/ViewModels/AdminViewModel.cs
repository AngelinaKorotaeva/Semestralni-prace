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
                            // Load students with includes
                            var query = _db.Student
                                .Include(s => s.Stravnik).ThenInclude(str => str.Alergie).ThenInclude(sa => sa.Alergie)
                                .Include(s => s.Stravnik).ThenInclude(str => str.Omezeni).ThenInclude(so => so.DietniOmezeni)
                                .Include(s => s.Trida)
                                .AsQueryable();
                            if (!string.IsNullOrEmpty(_selectedClass) && _selectedClass != "Vše")
                            {
                                if (int.TryParse(_selectedClass, out int cislo))
                                {
                                    query = query.Where(s => s.Trida.CisloTridy == cislo);
                                }
                            }
                            var students = await query.Take(100).ToListAsync();
                            var studentItems = new List<ItemViewModel>();
                            foreach (var student in students)
                            {
                                var alergie = student.Stravnik.Alergie != null ? string.Join(", ", student.Stravnik.Alergie.Select(sa => sa.Alergie.Nazev)) : "Žádné";
                                var omezeni = student.Stravnik.Omezeni != null ? string.Join(", ", student.Stravnik.Omezeni.Select(so => so.DietniOmezeni.Nazev)) : "Žádné";
                                var summary = $"{student.Stravnik.Jmeno} {student.Stravnik.Prijmeni} - {student.Stravnik.Email} - {student.DatumNarozeni.ToShortDateString()} - Alergie: {alergie} - Omezení: {omezeni}";
                                studentItems.Add(new ItemViewModel(student, summary));
                            }
                            return studentItems;
                        }
                        else if (entityType == typeof(Pracovnik))
                        {
                            // Load workers with includes
                            var query = _db.Pracovnik
                                .Include(p => p.Stravnik).ThenInclude(str => str.Alergie).ThenInclude(sa => sa.Alergie)
                                .Include(p => p.Stravnik).ThenInclude(str => str.Omezeni).ThenInclude(so => so.DietniOmezeni)
                                .Include(p => p.Pozice)
                                .AsQueryable();
                            if (!string.IsNullOrEmpty(_selectedPosition) && _selectedPosition != "Vše")
                            {
                                query = query.Where(p => p.Pozice != null && p.Pozice.Nazev == _selectedPosition);
                            }
                            var workers = await query.Take(100).ToListAsync();
                            var workerItems = new List<ItemViewModel>();
                            foreach (var worker in workers)
                            {
                                var pozice = worker.Pozice?.Nazev ?? "Nezadáno";
                                var alergie = worker.Stravnik.Alergie != null ? string.Join(", ", worker.Stravnik.Alergie.Select(sa => sa.Alergie.Nazev)) : "Žádné";
                                var omezeni = worker.Stravnik.Omezeni != null ? string.Join(", ", worker.Stravnik.Omezeni.Select(so => so.DietniOmezeni.Nazev)) : "Žádné";
                                var summary = $"{worker.Stravnik.Jmeno} {worker.Stravnik.Prijmeni} - {worker.Stravnik.Email} - Telefon: {worker.Telefon} - Pozice: {pozice} - Alergie: {alergie} - Omezení: {omezeni}";
                                workerItems.Add(new ItemViewModel(worker, summary));
                            }
                            return workerItems;
                        }
                        else if (entityType == typeof(Jidlo))
                        {
                            // Load foods with includes
                            var query = _db.Jidlo
                                .Include(j => j.SlozkyJidla).ThenInclude(sj => sj.Slozka)
                                .AsQueryable();
                            if (!string.IsNullOrEmpty(_selectedFoodCategory) && _selectedFoodCategory != "Vše")
                            {
                                query = query.Where(j => j.Kategorie == _selectedFoodCategory);
                            }
                            var foods = await query.Take(100).ToListAsync();
                            var foodItems = new List<ItemViewModel>();
                            foreach (var food in foods)
                            {
                                var slozeni = food.SlozkyJidla != null ? string.Join(", ", food.SlozkyJidla.Select(sj => sj.Slozka?.Nazev ?? "Nezadáno")) : "Nezadáno";
                                var summary = $"{food.Nazev} - {food.Popis} - Cena: {food.Cena} - Složení: {slozeni}";
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

        if (item.Entity is Student student)
        {
            // Jméno
            Properties.Add(new PropertyViewModel("Jméno", typeof(string), student.Stravnik.Jmeno, val => student.Stravnik.Jmeno = (string)val));

            // Příjmení
            Properties.Add(new PropertyViewModel("Příjmení", typeof(string), student.Stravnik.Prijmeni, val => student.Stravnik.Prijmeni = (string)val));

            // Datum narození
            Properties.Add(new PropertyViewModel("Datum narození", typeof(DateTime), student.DatumNarozeni.Date, val => student.DatumNarozeni = ((DateTime)val).Date));

            // Adresa
            Properties.Add(new PropertyViewModel("Adresa", typeof(string), $"{student.Stravnik.Adresa?.Ulice ?? ""}, {student.Stravnik.Adresa?.Mesto ?? ""} {student.Stravnik.Adresa?.Psc.ToString() ?? ""}", null));

            // Třída
            var tridy = _db.Trida.ToList();
            Properties.Add(new PropertyViewModel("Třída", typeof(Trida), student.Trida, val => { if (val is Trida t) student.IdTrida = t.IdTrida; }) { EditorType = "Combo", ItemsSource = tridy });

            // Alergie
            var allAlergies = _db.Alergie.ToList();
            var selectableAlergies = allAlergies.Select(a => new SelectableItem(a, student.Stravnik.Alergie.Any(sa => sa.IdAlergie == a.IdAlergie))).ToList();
            Properties.Add(new PropertyViewModel("Alergie", typeof(IEnumerable<SelectableItem>), selectableAlergies, null) { EditorType = "MultiSelect", ItemsSource = selectableAlergies });

            // Dietní omezení
            var allOmezeni = _db.DietniOmezeni.ToList();
            var selectableOmezeni = allOmezeni.Select(o => new SelectableItem(o, student.Stravnik.Omezeni.Any(so => so.IdOmezeni == o.IdOmezeni))).ToList();
            Properties.Add(new PropertyViewModel("Dietní omezení", typeof(IEnumerable<SelectableItem>), selectableOmezeni, null) { EditorType = "MultiSelect", ItemsSource = selectableOmezeni });
        }
        else
        {
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

            // Special handling for Student
            if (SelectedItem.Entity is Student student)
            {
                // Update alergie
                var alergieProp = Properties.FirstOrDefault(p => p.Name == "Alergie");
                if (alergieProp?.ItemsSource != null)
                {
                    var selectedAlergies = alergieProp.ItemsSource.Cast<SelectableItem>().Where(si => si.IsSelected).Select(si => (Alergie)si.Item).ToList();
                    var currentAlergies = student.Stravnik.Alergie.Select(sa => sa.Alergie).ToList();

                    // Remove deselected
                    var toRemove = student.Stravnik.Alergie.Where(sa => !selectedAlergies.Any(a => a.IdAlergie == sa.IdAlergie)).ToList();
                    foreach (var sa in toRemove) _db.StravnikAlergie.Remove(sa);

                    // Add selected
                    var toAdd = selectedAlergies.Where(a => !student.Stravnik.Alergie.Any(sa => sa.IdAlergie == a.IdAlergie)).ToList();
                    foreach (var a in toAdd)
                    {
                        _db.StravnikAlergie.Add(new StravnikAlergie { IdStravnik = student.IdStravnik, IdAlergie = a.IdAlergie });
                    }
                }

                // Update omezeni
                var omezeniProp = Properties.FirstOrDefault(p => p.Name == "Dietní omezení");
                if (omezeniProp?.ItemsSource != null)
                {
                    var selectedOmezeni = omezeniProp.ItemsSource.Cast<SelectableItem>().Where(si => si.IsSelected).Select(si => (DietniOmezeni)si.Item).ToList();

                    // Remove deselected
                    var toRemove = student.Stravnik.Omezeni.Where(so => !selectedOmezeni.Any(o => o.IdOmezeni == so.IdOmezeni)).ToList();
                    foreach (var so in toRemove) _db.StravnikOmezeni.Remove(so);

                    // Add selected
                    var toAdd = selectedOmezeni.Where(o => !student.Stravnik.Omezeni.Any(so => so.IdOmezeni == o.IdOmezeni)).ToList();
                    foreach (var o in toAdd)
                    {
                        _db.StravnikOmezeni.Add(new StravnikOmezeni { IdStravnik = student.IdStravnik, IdOmezeni = o.IdOmezeni });
                    }
                }

                await _db.SaveChangesAsync();
            }

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
            if (SelectedItem.Entity is Student student)
            {
                // Remove alergie links
                var alergieLinks = student.Stravnik.Alergie.ToList();
                foreach (var sa in alergieLinks) _db.StravnikAlergie.Remove(sa);

                // Remove omezeni links
                var omezeniLinks = student.Stravnik.Omezeni.ToList();
                foreach (var so in omezeniLinks) _db.StravnikOmezeni.Remove(so);

                // Then remove student
                _db.Remove(student);
            }
            else if (SelectedItem.Entity is Pracovnik pracovnik)
            {
                // Remove alergie links
                var alergieLinks = pracovnik.Stravnik.Alergie.ToList();
                foreach (var sa in alergieLinks) _db.StravnikAlergie.Remove(sa);

                // Remove omezeni links
                var omezeniLinks = pracovnik.Stravnik.Omezeni.ToList();
                foreach (var so in omezeniLinks) _db.StravnikOmezeni.Remove(so);

                // Then remove pracovnik
                _db.Remove(pracovnik);
            }
            else if (SelectedItem.Entity is Jidlo jidlo)
            {
                // Remove slozky links
                var slozkyLinks = jidlo.SlozkyJidla.ToList();
                foreach (var sj in slozkyLinks) _db.SlozkaJidlo.Remove(sj);

                // Then remove jidlo
                _db.Remove(jidlo);
            }
            else
            {
                _db.Remove(SelectedItem.Entity);
            }

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