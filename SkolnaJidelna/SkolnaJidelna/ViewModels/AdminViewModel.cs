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

// ViewModel pro administrátorský panel.
// Zajišťuje:
// - výběr typu entity (Studenti/Pracovníci/Jídla/Složky/Alergie a omezení/Menu/Soubory/Objednavky),
// - načítání položek s filtrováním (třída/pozice/kategorie/typ),
// - zobrazení a editaci vlastností vybrané položky,
// - vytvoření nového studenta/pracovníka,
// - uložení a mazání záznamů.
public class AdminViewModel : INotifyPropertyChanged
{
    private readonly AppDbContext _db;

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // Seznam dostupných typů entit v admin panelu
    public ObservableCollection<EntityTypeDescriptor> EntityTypes { get; } = new();
    // Načtené položky aktuálně zvoleného typu
    public ObservableCollection<ItemViewModel> Items { get; } = new();
    // Editovatelné vlastnosti vybrané položky
    public ObservableCollection<PropertyViewModel> Properties { get; } = new();
    // Sloupce tabulky (fallback pro ostatní entity)
    public ObservableCollection<string> TableProperties { get; } = new();
    // Pomocné kolekce pro filtry
    public ObservableCollection<string> Classes { get; } = new();
    public ObservableCollection<string> Positions { get; } = new();
    public ObservableCollection<string> FoodCategories { get; } = new();
    public ObservableCollection<string> DietTypes { get; } = new();
    public ObservableCollection<string> MenuCategories { get; } = new();
    public ObservableCollection<string> ObjednavkyStav { get; } = new();

    // Aktuální seznam pro zobrazení v levém panelu (dynamicky ukazuje třídy/pozice/kategorie/sloupce)
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

    // Vybraný typ entity – mění filtry a spouští načtení položek
    private EntityTypeDescriptor? _selectedEntityType;
    public EntityTypeDescriptor? SelectedEntityType
    {
        get => _selectedEntityType;
        set
        {
            if (_selectedEntityType == value) return;
            _selectedEntityType = value;

            // Reset filtrů a příprava odpovídajících seznamů podle vybraného typu
            TableProperties.Clear();
            Classes.Clear();
            Positions.Clear();
            FoodCategories.Clear();
            DietTypes.Clear();
            MenuCategories.Clear();
            ObjednavkyStav.Clear();

            CurrentList = null;

            _selectedClass = null;
            _selectedPosition = null;
            _selectedFoodCategory = null;
            _selectedDietType = null;
            _selectedOrderState = null;

            Raise(nameof(SelectedClass));
            Raise(nameof(SelectedPosition));
            Raise(nameof(SelectedFoodCategory));
            Raise(nameof(SelectedDietType));
            Raise(nameof(SelectedOrderState));

            TableProperties.Clear();
            Classes.Clear();
            Positions.Clear();
            FoodCategories.Clear();
            MenuCategories.Clear();
            ObjednavkyStav.Clear();
            if (_selectedEntityType != null)
            {
                if (_selectedEntityType.Name == "Studenti")
                {
                    // Studenti – načti seznam tříd
                    _ = LoadClassesAsync();
                    CurrentList = Classes;
                }
                else if (_selectedEntityType.Name == "Pracovníci")
                {
                    // Pracovníci – načti seznam pozic
                    _ = LoadPositionsAsync();
                    CurrentList = Positions;
                }
                else if (_selectedEntityType.Name == "Jídla")
                {
                    // Jídla – načti seznam kategorií jídel
                    _ = LoadFoodCategoriesAsync();
                    CurrentList = FoodCategories;
                }
                else if (_selectedEntityType.Name == "Alergie a omezení")
                {
                    // Předvolby pro zobrazení alergií vs. dietních omezení
                    DietTypes.Clear();
                    DietTypes.Add("Vše");
                    DietTypes.Add("Alergie");
                    DietTypes.Add("Dietní omezení");
                    CurrentList = DietTypes;
                } 
                else if (_selectedEntityType.Name == "Menu")
                {
                    // Typ menu
                    MenuCategories.Clear();
                    MenuCategories.Add("Vše");
                    MenuCategories.Add("Snidaně");
                    MenuCategories.Add("Oběd");
                    CurrentList = MenuCategories;
                    SelectedMenuType = null;
                }
                else if (_selectedEntityType.Name == "Složky")
                {
                    // inicializujeme novou kolekci
                    CurrentList = new ObservableCollection<string>();
                    CurrentList.Add("Vše");
                }
                else if (_selectedEntityType.Name == "Soubory")
                {
                    CurrentList = new ObservableCollection<string>();
                    CurrentList.Add("Vše");
                }
                else if (_selectedEntityType.Name == "Objednavky")
                {
                    _ = LoadObjednavkyStavAsync();
                    CurrentList = ObjednavkyStav;
                }
                else
                {
                    // Ostatní entity – zobraz jejich sloupce
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
            // Automatické načtení položek po změně typu entity
            _ = LoadItemsForSelectedEntityAsync();
        }
    }

    // Vybraná položka – aktualizace panelu vlastností
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

    // Filtry (změna vyvolá znovunačtení položek)
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

    private string? _selectedMenuType;
    public string? SelectedMenuType
    {
        get => _selectedMenuType;
        set
        {
            if (_selectedMenuType == value) return;
            _selectedMenuType = value;
            Raise(nameof(SelectedMenuType));
            _ = LoadItemsForSelectedEntityAsync();
        }
    }

    private string? _selectedOrderState;
    public string? SelectedOrderState
    {
        get => _selectedOrderState;
        set
        {
            if (_selectedOrderState == value) return;
            _selectedOrderState = value;
            Raise(nameof(SelectedOrderState));
            _ = LoadItemsForSelectedEntityAsync();
        }
    }

    // Příkazy pro UI
    public ICommand RefreshCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand DeleteCommand { get; }

    public AdminViewModel(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        // Obnovení dat/typů, uložení změn vybrané položky, zavření okna (řeší view), smazání položky
        RefreshCommand = new RelayCommand(async _ => await LoadEntityTypesAsync());
        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => SelectedItem != null);
        CloseCommand = new RelayCommand(_ => { /* zavření okna řeší view */ });
        DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => SelectedItem != null);

        // Statické nadefinování typů entit zobrazovaných v panelu a jejich načítacích rutin
        var entityTypesToInclude = new[]
        {
            ("Studenti", typeof(Student)),
            ("Pracovníci", typeof(Pracovnik)),
            ("Jídla", typeof(Jidlo)),
            ("Složky", typeof(Slozka)),
            ("Alergie a omezení", typeof(DietniOmezeni)),
            ("Menu", typeof(Menu)),
            ("Soubory", typeof(Soubor)),
            ("Objednavky", typeof(Objednavka))
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
                        // Obecné získání DbSet<T>
                        var setMethod = typeof(DbContext).GetMethods()
                            .Where(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 0)
                            .FirstOrDefault()?.MakeGenericMethod(entityType);
                        var dbSet = setMethod?.Invoke(_db, null) as IQueryable;
                        if (dbSet == null) return new List<ItemViewModel>();

                        // Větvení podle typu entity – cílené dotazy + filtry
                        if (entityType == typeof(Student))
                        {
                            var query = _db.Student
                                .Include(s => s.Stravnik).ThenInclude(str => str.Adresa)
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
                            var query = _db.Pracovnik
                                .Include(p => p.Stravnik).ThenInclude(str => str.Adresa)
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
                            // Kombinované zobrazení: Alergie i Dietní omezení podle výběru
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
                        } else if (entityType == typeof(Menu))
                        {
                            var query = _db.Menu
                                .Include(m => m.Jidla)
                                .AsQueryable();

                            if (!string.IsNullOrEmpty(_selectedMenuType) && _selectedMenuType != "Vše")
                            {
                                var typ = _selectedMenuType.Trim().ToUpperInvariant();
                                // Map UI labels to DB values
                                if (typ == "SNIDANĚ" || typ == "SNIDANE") typ = "SNIDANE";
                                else if (typ == "OBĚD" || typ == "OBED") typ = "OBED";
                                query = query.Where(m => m.TypMenu != null && m.TypMenu.ToUpper() == typ);
                            }

                            var menus = await query
                                .OrderBy(m => m.IdMenu)
                                .Take(100)
                                .ToListAsync();

                            var menuItems = new List<ItemViewModel>();
                            foreach (var m in menus)
                            {
                                var pocetJidel = m.Jidla?.Count ?? 0;
                                var typText = string.IsNullOrWhiteSpace(m.TypMenu) ? "Nezadáno" : m.TypMenu;
                                var summary = $"{m.Nazev} - {typText} - Od: {m.TimeOd:dd.MM.yyyy} - Do: {m.TimeDo:dd.MM.yyyy} - Počet jídel: {pocetJidel}";
                                menuItems.Add(new ItemViewModel(m, summary));
                            }
                            return menuItems;
                        } else if (entityType == typeof(Slozka))
                        {
                            // Zobrazení složek (ingrediencí) s počtem jídel, ve kterých se používají
                            var slozky = await _db.Slozka
                                .AsNoTracking()
                                .OrderBy(s => s.Nazev)
                                .Take(200)
                                .ToListAsync();

                            // Načti mapu počtů použití složek v jídelníčku
                            var usageCounts = await _db.SlozkaJidlo
                                .AsNoTracking()
                                .GroupBy(sj => sj.IdSlozka)
                                .Select(g => new { IdSlozka = g.Key, Count = g.Count() })
                                .ToDictionaryAsync(x => x.IdSlozka, x => x.Count);

                            var slozkaItems = new List<ItemViewModel>();
                            foreach (var s in slozky)
                            {
                                var count = 0;
                                if (usageCounts.TryGetValue(s.IdSlozka, out var c)) count = c;
                                var summary = string.IsNullOrWhiteSpace(s.Nazev)
                                    ? $"Složka {s.IdSlozka} - Použito v {count} jídlech"
                                    : $"{s.Nazev} - Použito v {count} jídlech";
                                slozkaItems.Add(new ItemViewModel(s, summary));
                            }
                            return slozkaItems;
                        }
                        else if (entityType == typeof(Soubor))
                        {
                            // Zobrazení souborů: název, přípona, typ, cílová tabulka, vlastník a datum nahrání
                            var files = await _db.Soubor
                                .Include(s => s.Stravnik)
                                .AsNoTracking()
                                .OrderByDescending(s => s.DatumNahrani)
                                .Take(100)
                                .ToListAsync();

                            var fileItems = new List<ItemViewModel>();
                            foreach (var f in files)
                            {
                                var owner = f.Stravnik?.Email;
                                if (string.IsNullOrWhiteSpace(owner))
                                {
                                    var jm = (f.Stravnik?.Jmeno ?? string.Empty).Trim();
                                    var pr = (f.Stravnik?.Prijmeni ?? string.Empty).Trim();
                                    owner = string.Join(" ", new[] { jm, pr }.Where(x => !string.IsNullOrWhiteSpace(x)));
                                }
                                var sizeKb = f.Obsah != null ? ($" {(f.Obsah.Length / 1024.0):F1} KB") : string.Empty;
                                var nameWithExt = string.IsNullOrWhiteSpace(f.Pripona) ? f.Nazev : $"{f.Nazev}.{f.Pripona}";
                                var summary = $"{nameWithExt} - {f.Typ} - {f.Tabulka}{sizeKb} - {f.DatumNahrani:dd.MM.yyyy}";
                                if (!string.IsNullOrWhiteSpace(owner)) summary += $" - {owner}";
                                fileItems.Add(new ItemViewModel(f, summary));
                            }
                            return fileItems;
                        }
                        else if (entityType == typeof(Objednavka))
                        {
                            var query = _db.Objednavka
                                .Include(o => o.Stravnik)
                                .Include(o => o.Stav)
                                .Include(o => o.Polozky)
                                .AsQueryable();

                            if (!string.IsNullOrWhiteSpace(_selectedOrderState) && _selectedOrderState != "Vše")
                            {
                                query = query.Where(o => o.Stav != null && o.Stav.Nazev == _selectedOrderState);
                            }

                            var orders = await query
                                .OrderByDescending(o => o.Datum)
                                .Take(200)
                                .ToListAsync();

                            var orderItems = new List<ItemViewModel>();
                            foreach (var o in orders)
                            {
                                var user = o.Stravnik?.Email ?? ($"{o.Stravnik?.Jmeno} {o.Stravnik?.Prijmeni}".Trim());
                                var polCount = o.Polozky?.Count ?? 0;
                                var summary = $"#{o.IdObjednavka} - {o.Datum:dd.MM.yyyy} - {o.Stav?.Nazev ?? "Nezadáno"} - {o.CelkovaCena} Kč - {user} - Položek: {polCount}";
                                orderItems.Add(new ItemViewModel(o, summary));
                            }
                            return orderItems;
                        }

                            // Fallback – načti obecně (omezeno na 100 záznamů)
                            var toListAsyncMethod = typeof(Queryable).GetMethods()
                                .Where(m => m.Name == "ToList" && m.GetParameters().Length == 1 && m.GetGenericArguments().Length == 1)
                                .FirstOrDefault()?.MakeGenericMethod(entityType);
                        if (toListAsyncMethod == null) return new List<ItemViewModel>();

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
                        if (list == null) return new List<ItemViewModel>();

                        var items = new List<ItemViewModel>();
                        foreach (var item in list)
                        {
                            var summary = $"{displayName} {GetIdValue(item)}";
                            items.Add(new ItemViewModel(item, summary));
                        }
                        return items;
                    }
                    catch (Exception)
                    {
                        return new List<ItemViewModel>();
                    }
                }
            });
        }
    }

    // Inicializace typů entit a první načtení položek (pokud již je vybrán typ)
    public async Task LoadEntityTypesAsync()
    {
        if (EntityTypes.Count == 0) return;
        await LoadItemsForSelectedEntityAsync();
    }

    // Načte položky podle právě vybraného typu a aktivních filtrů
    public async Task LoadItemsForSelectedEntityAsync()
    {
        Items.Clear();
        Properties.Clear();
        SelectedItem = null;

        if (SelectedEntityType?.LoaderAsync == null) return;
        var items = await SelectedEntityType.LoaderAsync();
        foreach (var it in items) Items.Add(it);
    }

    // Příprava nového studenta – vytvoří prázdné objekty a nastaví do editoru
    public async Task<bool> BeginCreateStudentAsync()
    {
        var trida = await _db.Trida.OrderBy(t => t.CisloTridy).FirstOrDefaultAsync();
        if (trida == null)
        {
            MessageBox.Show("Nelze vytvořit studenta: žádná třída není v databázi.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        var adresa = new Adresa { Psc = 0, Mesto = string.Empty, Ulice = string.Empty };
        var stravnik = new Stravnik
        {
            Jmeno = string.Empty,
            Prijmeni = string.Empty,
            Email = string.Empty,
            Heslo = string.Empty,
            Zustatek = 0,
            Role = "user",
            Aktivita = '1',
            TypStravnik = "st",
            Adresa = adresa,
            Alergie = new List<StravnikAlergie>(),
            Omezeni = new List<StravnikOmezeni>()
        };

        var student = new Student
        {
            Stravnik = stravnik,
            DatumNarozeni = DateTime.Today,
            Trida = trida,
            IdTrida = trida.IdTrida
        };

        SelectedItem = new ItemViewModel(student, "Nový student");
        OnSelectedItemChanged(SelectedItem);
        return true;
    }

    // Příprava nového pracovníka – vytvoří prázdné objekty a nastaví do editoru
    public async Task<bool> BeginCreateWorkerAsync()
    {
        var pozice = await _db.Pozice.OrderBy(p => p.IdPozice).FirstOrDefaultAsync();
        if (pozice == null)
        {
            MessageBox.Show("Nelze vytvořit pracovníka: žádná pozice není v databázi.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        var adresa = new Adresa { Psc = 0, Mesto = string.Empty, Ulice = string.Empty };
        var stravnik = new Stravnik
        {
            Jmeno = string.Empty,
            Prijmeni = string.Empty,
            Email = string.Empty,
            Heslo = string.Empty,
            Zustatek = 0,
            Role = "user",
            Aktivita = '1',
            TypStravnik = "pr",
            Adresa = adresa,
            Alergie = new List<StravnikAlergie>(),
            Omezeni = new List<StravnikOmezeni>()
        };

        var pracovnik = new Pracovnik
        {
            Stravnik = stravnik,
            Telefon = 0,
            Pozice = pozice,
            IdPozice = pozice.IdPozice
        };

        SelectedItem = new ItemViewModel(pracovnik, "Nový pracovník");
        OnSelectedItemChanged(SelectedItem);
        return true;
    }

    // Reakce na výběr položky – připraví PropertyViewModely podle typu entity
    public void OnSelectedItemChanged(ItemViewModel? item)
    {
        Properties.Clear();
        if (item == null) return;

        // Speciální editor pro Student/Pracovník (vč. adresy, tříd/pozic, alergií a diet)
        // Ostatní entity se generují obecně podle scalarních vlastností
        if (item.Entity is Student student)
        {
            student.Stravnik.Alergie ??= new List<StravnikAlergie>();
            student.Stravnik.Omezeni ??= new List<StravnikOmezeni>();
            if (student.Stravnik.Adresa == null) student.Stravnik.Adresa = new Adresa();

            // Jméno
            Properties.Add(new PropertyViewModel("Jméno", typeof(string), student.Stravnik.Jmeno, val => student.Stravnik.Jmeno = (string)val));

            // Příjmení
            Properties.Add(new PropertyViewModel("Příjmení", typeof(string), student.Stravnik.Prijmeni, val => student.Stravnik.Prijmeni = (string)val));

            // Email / Heslo pouze pro nové záznamy
            if (student.IdStravnik == 0)
            {
                Properties.Add(new PropertyViewModel("Email", typeof(string), student.Stravnik.Email, val => student.Stravnik.Email = val?.ToString() ?? string.Empty));
                Properties.Add(new PropertyViewModel("Heslo", typeof(string), student.Stravnik.Heslo ?? string.Empty, val => student.Stravnik.Heslo = val?.ToString() ?? string.Empty));
            }

            // Datum narození (date only)
            Properties.Add(new PropertyViewModel("Datum narození", typeof(DateTime), student.DatumNarozeni.Date, val => student.DatumNarozeni = ((DateTime)val).Date)
            {
                EditorType = "Date"
            });

            // Adresa - editable parts
            Properties.Add(new PropertyViewModel("PSČ", typeof(int), student.Stravnik.Adresa?.Psc ?? 0, val =>
            {
                if (val == null) { student.Stravnik.Adresa.Psc = 0; return; }
                var text = val.ToString();
                if (int.TryParse(text, out var psc)) student.Stravnik.Adresa.Psc = psc;
            }));
            Properties.Add(new PropertyViewModel("Město", typeof(string), student.Stravnik.Adresa?.Mesto ?? string.Empty, val => { student.Stravnik.Adresa.Mesto = val?.ToString() ?? string.Empty; }));
            Properties.Add(new PropertyViewModel("Ulice", typeof(string), student.Stravnik.Adresa?.Ulice ?? string.Empty, val => { student.Stravnik.Adresa.Ulice = val?.ToString() ?? string.Empty; }));

            // Třída
            var tridy = _db.Trida.ToList();
            Properties.Add(new PropertyViewModel("Třída", typeof(Trida), student.Trida, val => { if (val is Trida t) student.IdTrida = t.IdTrida; }) { EditorType = "Combo", ItemsSource = tridy });

            // Alergie (checkbox list)
            var allAlergies = _db.Alergie.ToList();
            var selectableAlergies = allAlergies.Select(a => new SelectableItem(a, student.Stravnik.Alergie.Any(sa => sa.IdAlergie == a.IdAlergie))).ToList();
            Properties.Add(new PropertyViewModel("Alergie", typeof(IEnumerable<SelectableItem>), selectableAlergies, null) { EditorType = "CheckboxList", ItemsSource = selectableAlergies });

            // Dietní omezení (checkbox list)
            var allOmezeni = _db.DietniOmezeni.ToList();
            var selectableOmezeni = allOmezeni.Select(o => new SelectableItem(o, student.Stravnik.Omezeni.Any(so => so.IdOmezeni == o.IdOmezeni))).ToList();
            Properties.Add(new PropertyViewModel("Dietní omezení", typeof(IEnumerable<SelectableItem>), selectableOmezeni, null) { EditorType = "CheckboxList", ItemsSource = selectableOmezeni });
        }
        else if (item.Entity is Pracovnik pracovnik)
        {
            pracovnik.Stravnik.Alergie ??= new List<StravnikAlergie>();
            pracovnik.Stravnik.Omezeni ??= new List<StravnikOmezeni>();
            if (pracovnik.Stravnik.Adresa == null) pracovnik.Stravnik.Adresa = new Adresa();

            Properties.Add(new PropertyViewModel("Jméno", typeof(string), pracovnik.Stravnik.Jmeno, val => pracovnik.Stravnik.Jmeno = val?.ToString() ?? string.Empty));
            Properties.Add(new PropertyViewModel("Příjmení", typeof(string), pracovnik.Stravnik.Prijmeni, val => pracovnik.Stravnik.Prijmeni = val?.ToString() ?? string.Empty));

            if (pracovnik.IdStravnik == 0)
            {
                Properties.Add(new PropertyViewModel("Email", typeof(string), pracovnik.Stravnik.Email, val => pracovnik.Stravnik.Email = val?.ToString() ?? string.Empty));
                Properties.Add(new PropertyViewModel("Heslo", typeof(string), pracovnik.Stravnik.Heslo ?? string.Empty, val => pracovnik.Stravnik.Heslo = val?.ToString() ?? string.Empty));
            }

            Properties.Add(new PropertyViewModel("Telefon", typeof(int), pracovnik.Telefon, val => { if (int.TryParse(val?.ToString(), out var tel)) pracovnik.Telefon = tel; else pracovnik.Telefon = 0; }));

            // Pozice combo
            var pozice = _db.Pozice.ToList();
            // default to first pozice if none selected
            if (pracovnik.Pozice == null && pozice.Count > 0)
            {
                pracovnik.Pozice = pozice[0];
                pracovnik.IdPozice = pozice[0].IdPozice;
            }
            Properties.Add(new PropertyViewModel("Pozice", typeof(Pozice), pracovnik.Pozice, val => { if (val is Pozice p) { pracovnik.Pozice = p; pracovnik.IdPozice = p.IdPozice; } }) { EditorType = "Combo", ItemsSource = pozice });

            // Adresa - editable parts
            Properties.Add(new PropertyViewModel("PSČ", typeof(int), pracovnik.Stravnik.Adresa?.Psc ?? 0, val => { if (val != null && int.TryParse(val.ToString(), out var psc)) pracovnik.Stravnik.Adresa.Psc = psc; }));
            Properties.Add(new PropertyViewModel("Město", typeof(string), pracovnik.Stravnik.Adresa?.Mesto ?? string.Empty, val => { pracovnik.Stravnik.Adresa.Mesto = val?.ToString() ?? string.Empty; }));
            Properties.Add(new PropertyViewModel("Ulice", typeof(string), pracovnik.Stravnik.Adresa?.Ulice ?? string.Empty, val => { pracovnik.Stravnik.Adresa.Ulice = val?.ToString() ?? string.Empty; }));

            // Alergie
            var allAlergies = _db.Alergie.ToList();
            var selectableAlergies = allAlergies.Select(a => new SelectableItem(a, pracovnik.Stravnik.Alergie.Any(sa => sa.IdAlergie == a.IdAlergie))).ToList();
            Properties.Add(new PropertyViewModel("Alergie", typeof(IEnumerable<SelectableItem>), selectableAlergies, null) { EditorType = "CheckboxList", ItemsSource = selectableAlergies });

            // Dietní omezení
            var allOmezeni = _db.DietniOmezeni.ToList();
            var selectableOmezeni = allOmezeni.Select(o => new SelectableItem(o, pracovnik.Stravnik.Omezeni.Any(so => so.IdOmezeni == o.IdOmezeni))).ToList();
            Properties.Add(new PropertyViewModel("Dietní omezení", typeof(IEnumerable<SelectableItem>), selectableOmezeni, null) { EditorType = "CheckboxList", ItemsSource = selectableOmezeni });
        }
        else if (item.Entity is Menu menu)
        {
            // Název
            Properties.Add(new PropertyViewModel("Název", typeof(string), menu.Nazev ?? string.Empty, val => menu.Nazev = val?.ToString() ?? string.Empty));

            // Typ menu (SNIDANĚ / OBĚD)
            var typy = new[] { "SNIDANE", "OBED" };
            Properties.Add(new PropertyViewModel("Typ menu", typeof(string), string.IsNullOrWhiteSpace(menu.TypMenu) ? "SNIDANE" : menu.TypMenu,
                val => menu.TypMenu = (val?.ToString() ?? "SNIDANE").Trim().ToUpperInvariant())
            { EditorType = "Combo", ItemsSource = typy });

            // Datum od + čas od (model používá nenulové DateTime)
            var timeOd = menu.TimeOd;
            Properties.Add(new PropertyViewModel("Datum od", typeof(DateTime), timeOd.Date, v =>
            {
                var date = ((DateTime)v).Date;
                var tod = timeOd.TimeOfDay;
                menu.TimeOd = date.Add(tod);
            }) { EditorType = "Date" });
            Properties.Add(new PropertyViewModel("Čas od (HH:mm)", typeof(string), timeOd.ToString("HH:mm"), v =>
            {
                if (TimeSpan.TryParse(v?.ToString(), out var ts))
                {
                    var d = menu.TimeOd.Date;
                    menu.TimeOd = d.Add(ts);
                }
            }));

            // Datum do + čas do
            var timeDo = menu.TimeDo;
            Properties.Add(new PropertyViewModel("Datum do", typeof(DateTime), timeDo.Date, v =>
            {
                var date = ((DateTime)v).Date;
                var tod = timeDo.TimeOfDay;
                menu.TimeDo = date.Add(tod);
            }) { EditorType = "Date" });
            Properties.Add(new PropertyViewModel("Čas do (HH:mm)", typeof(string), timeDo.ToString("HH:mm"), v =>
            {
                if (TimeSpan.TryParse(v?.ToString(), out var ts))
                {
                    var d = menu.TimeDo.Date;
                    menu.TimeDo = d.Add(ts);
                }
            }));
        }
        else if (item.Entity is Objednavka objednavka)
        {
            // Datum objednávky (date only)
            Properties.Add(new PropertyViewModel("Datum objednávky", typeof(DateTime), objednavka.Datum.Date, v => objednavka.Datum = ((DateTime)v).Date)
            {
                EditorType = "Date"
            });

            // Stav objednávky (combo)
            var stavy = _db.Stav.ToList();
            if (objednavka.Stav == null && stavy.Count > 0)
            {
                objednavka.Stav = stavy[0];
                objednavka.IdStav = stavy[0].IdStav;
            }
            Properties.Add(new PropertyViewModel("Stav", typeof(Stav), objednavka.Stav, v => { if (v is Stav s) { objednavka.Stav = s; objednavka.IdStav = s.IdStav; } })
            { EditorType = "Combo", ItemsSource = stavy });

            // Cena celkem
            Properties.Add(new PropertyViewModel("Cena celkem", typeof(double), objednavka.CelkovaCena, v =>
            {
                if (v != null && double.TryParse(v.ToString(), out var d)) objednavka.CelkovaCena = d;
            }));

            // Poznámka
            Properties.Add(new PropertyViewModel("Poznámka", typeof(string), objednavka.Poznamka ?? string.Empty, v => objednavka.Poznamka = v?.ToString()));
        }
        else
        {
            var props = item.Entity.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string))
                .Where(p =>
                {
                    var targetType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                    // allow only value types (including enums) and string -> skip navigation/reference types
                    return targetType.IsValueType || targetType == typeof(string);
                })
                .Where(p =>
                {
                    // never edit IDs
                    if (p.Name.StartsWith("Id", StringComparison.OrdinalIgnoreCase)) return false;
                    // never edit emails
                    if (p.Name.Equals("Email", StringComparison.OrdinalIgnoreCase)) return false;
                    return true;
                })
                .ToArray();

            foreach (var p in props)
            {
                var currentValue = p.GetValue(item.Entity);
                Action<object?> onChanged = val =>
                {
                    try
                    {
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

    // Uloží změny vybrané položky (nebo vloží novou) včetně vazeb
    public async Task SaveAsync()
    {
        if (SelectedItem == null) return;
        try
        {
            if (SelectedItem.Entity is Student student)
            {
                student.Stravnik.Alergie ??= new List<StravnikAlergie>();
                student.Stravnik.Omezeni ??= new List<StravnikOmezeni>();
                if (student.Stravnik.Adresa == null) student.Stravnik.Adresa = new Adresa();

                // basic required fields
                if (string.IsNullOrWhiteSpace(student.Stravnik.Jmeno) || string.IsNullOrWhiteSpace(student.Stravnik.Prijmeni) || string.IsNullOrWhiteSpace(student.Stravnik.Email))
                {
                    MessageBox.Show("Vyplňte jméno, příjmení a email.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (student.IdStravnik == 0 && string.IsNullOrWhiteSpace(student.Stravnik.Heslo))
                {
                    MessageBox.Show("Zadejte heslo pro nového studenta.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string hashed = student.Stravnik.Heslo ?? string.Empty;
                if (!hashed.StartsWith("$2")) hashed = BCrypt.Net.BCrypt.HashPassword(hashed);

                // address parts
                var psc = student.Stravnik.Adresa?.Psc ?? 0;
                var mesto = student.Stravnik.Adresa?.Mesto ?? "Nezadáno";
                var ulice = student.Stravnik.Adresa?.Ulice ?? "Nezadáno";
                var tridaCislo = student.Trida?.CisloTridy ?? 0;

                if (student.IdStravnik == 0)
                {
                    // call stored procedure trans_register_student
                    var conn = _db.Database.GetDbConnection();
                    try
                    {
                        if (conn.State != System.Data.ConnectionState.Open) conn.Open();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandType = System.Data.CommandType.Text;
                        if (cmd is Oracle.ManagedDataAccess.Client.OracleCommand ocmd) ocmd.BindByName = true;
                        cmd.CommandText = "BEGIN trans_register_student(:p_psc, :p_mesto, :p_ulice, :p_jmeno, :p_prijmeni, :p_email, :p_heslo, :p_zustatek, :p_rok_narozeni, :p_cislo_tridy); END;";
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_psc", Oracle.ManagedDataAccess.Client.OracleDbType.Int32) { Value = psc });
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_mesto", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = (object)mesto ?? DBNull.Value });
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_ulice", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = (object)ulice ?? DBNull.Value });
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_jmeno", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = student.Stravnik.Jmeno });
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_prijmeni", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = student.Stravnik.Prijmeni });
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_email", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = student.Stravnik.Email });
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_heslo", Oracle.ManagedDataAccess.Client.OracleDbType.Varchar2) { Value = hashed });
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_zustatek", Oracle.ManagedDataAccess.Client.OracleDbType.Decimal) { Value = 0 });
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_rok_narozeni", Oracle.ManagedDataAccess.Client.OracleDbType.Date) { Value = student.DatumNarozeni });
                        cmd.Parameters.Add(new Oracle.ManagedDataAccess.Client.OracleParameter("p_cislo_tridy", Oracle.ManagedDataAccess.Client.OracleDbType.Int32) { Value = tridaCislo });
                        cmd.ExecuteNonQuery();
                    }
                    finally
                    {
                        try { if (conn.State == System.Data.ConnectionState.Open) conn.Close(); } catch { }
                    }

                    // reload student from DB to get ids
                    var dbStudent = _db.Student.Include(s => s.Stravnik).ThenInclude(st => st.Adresa).Include(s => s.Trida).FirstOrDefault(s => s.Stravnik.Email == student.Stravnik.Email);
                    if (dbStudent != null)
                    {
                        student.IdStravnik = dbStudent.IdStravnik;
                        student.Stravnik.IdStravnik = dbStudent.IdStravnik;
                        student.Stravnik.IdAdresa = dbStudent.Stravnik.IdAdresa;
                        student.Trida = dbStudent.Trida;
                        student.IdTrida = dbStudent.IdTrida;
                    }
                }
                else
                {
                    // update existing via EF
                    student.Stravnik.Heslo = hashed;
                    _db.Update(student.Stravnik.Adresa);
                    _db.Update(student.Stravnik);
                    _db.Update(student);
                    await _db.SaveChangesAsync();
                }

                // Update alergie links
                var alergieProp = Properties.FirstOrDefault(p => p.Name == "Alergie");
                if (alergieProp?.ItemsSource != null)
                {
                    var selectedAlergies = alergieProp.ItemsSource.Cast<SelectableItem>().Where(si => si.IsSelected).Select(si => (Alergie)si.Item).ToList();
                    var toRemove = student.Stravnik.Alergie.Where(sa => !selectedAlergies.Any(a => a.IdAlergie == sa.IdAlergie)).ToList();
                    foreach (var sa in toRemove) _db.StravnikAlergie.Remove(sa);
                    var toAdd = selectedAlergies.Where(a => !student.Stravnik.Alergie.Any(sa => sa.IdAlergie == a.IdAlergie)).ToList();
                    foreach (var a in toAdd)
                    {
                        _db.StravnikAlergie.Add(new StravnikAlergie { IdStravnik = student.IdStravnik, IdAlergie = a.IdAlergie });
                    }
                }

                // Update dietni omezeni links
                var omezeniProp = Properties.FirstOrDefault(p => p.Name == "Dietní omezení");
                if (omezeniProp?.ItemsSource != null)
                {
                    var selectedOmezeni = omezeniProp.ItemsSource.Cast<SelectableItem>().Where(si => si.IsSelected).Select(si => (DietniOmezeni)si.Item).ToList();
                    var toRemove = student.Stravnik.Omezeni.Where(so => !selectedOmezeni.Any(o => o.IdOmezeni == so.IdOmezeni)).ToList();
                    foreach (var so in toRemove) _db.StravnikOmezeni.Remove(so);
                    var toAdd = selectedOmezeni.Where(o => !student.Stravnik.Omezeni.Any(so => so.IdOmezeni == o.IdOmezeni)).ToList();
                    foreach (var o in toAdd)
                    {
                        _db.StravnikOmezeni.Add(new StravnikOmezeni { IdStravnik = student.IdStravnik, IdOmezeni = o.IdOmezeni });
                    }
                }

                await _db.SaveChangesAsync();
            }
            else if (SelectedItem.Entity is Pracovnik pracovnik)
            {
                pracovnik.Stravnik.Alergie ??= new List<StravnikAlergie>();
                pracovnik.Stravnik.Omezeni ??= new List<StravnikOmezeni>();
                if (pracovnik.Stravnik.Adresa == null) pracovnik.Stravnik.Adresa = new Adresa();

                if (string.IsNullOrWhiteSpace(pracovnik.Stravnik.Jmeno) || string.IsNullOrWhiteSpace(pracovnik.Stravnik.Prijmeni))
                {
                    MessageBox.Show("Vyplňte jméno a příjmení.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (pracovnik.IdStravnik == 0)
                {
                    if (string.IsNullOrWhiteSpace(pracovnik.Stravnik.Email) || string.IsNullOrWhiteSpace(pracovnik.Stravnik.Heslo))
                    {
                        MessageBox.Show("Vyplňte email a heslo pro nového pracovníka.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var hashed = pracovnik.Stravnik.Heslo ?? string.Empty;
                    if (!hashed.StartsWith("$2")) hashed = BCrypt.Net.BCrypt.HashPassword(hashed);
                    pracovnik.Stravnik.Heslo = hashed;
                    pracovnik.Stravnik.TypStravnik = "pr";
                    pracovnik.Stravnik.Role = "user";
                    pracovnik.Stravnik.Aktivita = '1';

                    // address
                    _db.Adresa.Add(pracovnik.Stravnik.Adresa);
                    await _db.SaveChangesAsync();
                    pracovnik.Stravnik.IdAdresa = pracovnik.Stravnik.Adresa.IdAdresa;

                    // stravnik
                    _db.Stravnik.Add(pracovnik.Stravnik);
                    await _db.SaveChangesAsync();

                    // pracovník
                    pracovnik.IdStravnik = pracovnik.Stravnik.IdStravnik;
                    if (pracovnik.Pozice != null) pracovnik.IdPozice = pracovnik.Pozice.IdPozice;
                    _db.Pracovnik.Add(pracovnik);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // update address
                    if (pracovnik.Stravnik.Adresa.IdAdresa == 0)
                    {
                        _db.Adresa.Add(pracovnik.Stravnik.Adresa);
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        _db.Update(pracovnik.Stravnik.Adresa);
                    }
                    pracovnik.Stravnik.IdAdresa = pracovnik.Stravnik.Adresa.IdAdresa;

                    if (pracovnik.Pozice != null) pracovnik.IdPozice = pracovnik.Pozice.IdPozice;

                    _db.Update(pracovnik.Stravnik);
                    _db.Update(pracovnik);
                    await _db.SaveChangesAsync();
                }

                // Update alergie links
                var alergieProp = Properties.FirstOrDefault(p => p.Name == "Alergie");
                if (alergieProp?.ItemsSource != null)
                {
                    var selectedAlergies = alergieProp.ItemsSource.Cast<SelectableItem>().Where(si => si.IsSelected).Select(si => (Alergie)si.Item).ToList();
                    var toRemove = pracovnik.Stravnik.Alergie.Where(sa => !selectedAlergies.Any(a => a.IdAlergie == sa.IdAlergie)).ToList();
                    foreach (var sa in toRemove) _db.StravnikAlergie.Remove(sa);
                    var toAdd = selectedAlergies.Where(a => !pracovnik.Stravnik.Alergie.Any(sa => sa.IdAlergie == a.IdAlergie)).ToList();
                    foreach (var a in toAdd)
                    {
                        _db.StravnikAlergie.Add(new StravnikAlergie { IdStravnik = pracovnik.IdStravnik, IdAlergie = a.IdAlergie });
                    }
                }

                // Update dietni omezeni links
                var omezeniProp = Properties.FirstOrDefault(p => p.Name == "Dietní omezení");
                if (omezeniProp?.ItemsSource != null)
                {
                    var selectedOmezeni = omezeniProp.ItemsSource.Cast<SelectableItem>().Where(si => si.IsSelected).Select(si => (DietniOmezeni)si.Item).ToList();
                    var toRemove = pracovnik.Stravnik.Omezeni.Where(so => !selectedOmezeni.Any(o => o.IdOmezeni == so.IdOmezeni)).ToList();
                    foreach (var so in toRemove) _db.StravnikOmezeni.Remove(so);
                    var toAdd = selectedOmezeni.Where(o => !pracovnik.Stravnik.Omezeni.Any(so => so.IdOmezeni == o.IdOmezeni)).ToList();
                    foreach (var o in toAdd)
                    {
                        _db.StravnikOmezeni.Add(new StravnikOmezeni { IdStravnik = pracovnik.IdStravnik, IdOmezeni = o.IdOmezeni });
                    }
                }

                await _db.SaveChangesAsync();
            }
            else if (SelectedItem.Entity is Menu menu)
            {
                // Speciální zpracování pro Menu
                if (string.IsNullOrWhiteSpace(menu.Nazev))
                {
                    MessageBox.Show("Vyplňte název menu.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Základní validace času
                if (menu.TimeOd == default || menu.TimeDo == default)
                {
                    MessageBox.Show("Vyplňte časové rozmezí (od-do) pro menu.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Výchozí typ menu na SNIDANE, pokud není oběd
                if (string.IsNullOrWhiteSpace(menu.TypMenu)) menu.TypMenu = "SNIDANE";

                var timeOd = menu.TimeOd;
                var timeDo = menu.TimeDo;

                // Kontrola časového rozmezí
                if (timeOd >= timeDo)
                {
                    MessageBox.Show("Čas \"od\" musí být před časem \"do\".", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Uložení přes EF Core
                if (menu.IdMenu == 0)
                {
                    _db.Menu.Add(menu);
                }
                else
                {
                    _db.Menu.Update(menu);
                }
                await _db.SaveChangesAsync();
            }
            else if (SelectedItem.Entity is Objednavka objednavka)
            {
                // Speciální editor pro Objednavku
                if (objednavka.IdObjednavka == 0)
                {
                    // Nová objednávka – povinné položky
                    if (string.IsNullOrWhiteSpace(objednavka.Stravnik?.Email))
                    {
                        MessageBox.Show("Vyplňte e-mail strávníka.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Uložení přes EF Core
                if (objednavka.IdObjednavka == 0)
                {
                    _db.Objednavka.Add(objednavka);
                }
                else
                {
                    _db.Objednavka.Update(objednavka);
                }
                await _db.SaveChangesAsync();
            }
            else
            {
                if (_db.Entry(SelectedItem.Entity).State == EntityState.Detached)
                {
                    _db.Add(SelectedItem.Entity);
                }
                else
                {
                    _db.Update(SelectedItem.Entity);
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

    // Smaže vybranou položku včetně souvisejících vazeb (podle typu)
    private async Task DeleteAsync()
    {
        if (SelectedItem == null) return;
        try
        {
            if (SelectedItem.Entity is Student student)
            {
                var fullStudent = await _db.Student
                    .Include(s => s.Stravnik).ThenInclude(str => str.Adresa)
                    .Include(s => s.Stravnik).ThenInclude(str => str.Alergie)
                    .Include(s => s.Stravnik).ThenInclude(str => str.Omezeni)
                    .Include(s => s.Stravnik).ThenInclude(str => str.Objednavky).ThenInclude(o => o.Polozky)
                    .Include(s => s.Stravnik).ThenInclude(str => str.Platby)
                    .Include(s => s.Stravnik).ThenInclude(str => str.Soubory)
                    .FirstOrDefaultAsync(s => s.IdStravnik == student.IdStravnik);

                if (fullStudent != null)
                {
                    foreach (var pol in fullStudent.Stravnik.Objednavky.SelectMany(o => o.Polozky))
                        _db.Remove(pol);

                    foreach (var obj in fullStudent.Stravnik.Objednavky)
                        _db.Remove(obj);

                    foreach (var plat in fullStudent.Stravnik.Platby)
                        _db.Remove(plat);

                    foreach (var soub in fullStudent.Stravnik.Soubory)
                        _db.Remove(soub);

                    foreach (var sa in fullStudent.Stravnik.Alergie)
                        _db.Remove(sa);

                    foreach (var so in fullStudent.Stravnik.Omezeni)
                        _db.Remove(so);

                    var adresaUsed = await _db.Stravnik
                        .AsNoTracking()
                        .CountAsync(str => str.IdAdresa == fullStudent.Stravnik.IdAdresa && str.IdStravnik != fullStudent.IdStravnik);
                    if (adresaUsed == 0)
                        _db.Remove(fullStudent.Stravnik.Adresa);

                    _db.Remove(fullStudent.Stravnik);
                    _db.Remove(fullStudent);
                }
            }
            else if (SelectedItem.Entity is Pracovnik pracovnik)
            {
                var fullPracovnik = await _db.Pracovnik
                    .Include(p => p.Stravnik).ThenInclude(str => str.Adresa)
                    .Include(p => p.Stravnik).ThenInclude(str => str.Alergie)
                    .Include(p => p.Stravnik).ThenInclude(str => str.Omezeni)
                    .Include(p => p.Stravnik).ThenInclude(str => str.Objednavky).ThenInclude(o => o.Polozky)
                    .Include(p => p.Stravnik).ThenInclude(str => str.Platby)
                    .Include(p => p.Stravnik).ThenInclude(str => str.Soubory)
                    .FirstOrDefaultAsync(p => p.IdStravnik == pracovnik.IdStravnik);

                if (fullPracovnik != null)
                {
                    foreach (var pol in fullPracovnik.Stravnik.Objednavky.SelectMany(o => o.Polozky))
                        _db.Remove(pol);

                    foreach (var obj in fullPracovnik.Stravnik.Objednavky)
                        _db.Remove(obj);

                    foreach (var plat in fullPracovnik.Stravnik.Platby)
                        _db.Remove(plat);

                    foreach (var soub in fullPracovnik.Stravnik.Soubory)
                        _db.Remove(soub);

                    foreach (var sa in fullPracovnik.Stravnik.Alergie)
                        _db.Remove(sa);

                    foreach (var so in fullPracovnik.Stravnik.Omezeni)
                        _db.Remove(so);

                    var adresaUsed = await _db.Stravnik
                        .AsNoTracking()
                        .CountAsync(str => str.IdAdresa == fullPracovnik.Stravnik.IdAdresa && str.IdStravnik != fullPracovnik.IdStravnik);
                    if (adresaUsed == 0)
                        _db.Remove(fullPracovnik.Stravnik.Adresa);

                    _db.Remove(fullPracovnik.Stravnik);
                    _db.Remove(fullPracovnik);
                }
            }
            else if (SelectedItem.Entity is Jidlo jidlo)
            {
                var fullJidlo = await _db.Jidlo
                    .Include(j => j.SlozkyJidla)
                    .Include(j => j.Polozky)
                    .FirstOrDefaultAsync(j => j.IdJidlo == jidlo.IdJidlo);

                if (fullJidlo != null)
                {
                    if (fullJidlo.Polozky != null)
                    {
                        foreach (var p in fullJidlo.Polozky) _db.Remove(p);
                    }

                    if (fullJidlo.SlozkyJidla != null)
                    {
                        foreach (var sj in fullJidlo.SlozkyJidla) _db.Remove(sj);
                    }

                    _db.Remove(fullJidlo);
                }
            }
            else if (SelectedItem.Entity is Alergie alergie)
            {
                var links = await _db.StravnikAlergie.Where(sa => sa.IdAlergie == alergie.IdAlergie).ToListAsync();
                foreach (var link in links) _db.Remove(link);

                var entity = await _db.Alergie.FirstOrDefaultAsync(a => a.IdAlergie == alergie.IdAlergie);
                if (entity != null) _db.Remove(entity);
            }
            else if (SelectedItem.Entity is DietniOmezeni omezeni)
            {
                var links = await _db.StravnikOmezeni.Where(so => so.IdOmezeni == omezeni.IdOmezeni).ToListAsync();
                foreach (var link in links) _db.Remove(link);

                var entity = await _db.DietniOmezeni.FirstOrDefaultAsync(o => o.IdOmezeni == omezeni.IdOmezeni);
                if (entity != null) _db.Remove(entity);
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

    // Načte seznam tříd pro filtr studenta
    public async Task LoadClassesAsync()
    {
        var classes = await _db.Trida.Select(t => t.CisloTridy).ToListAsync();
        Classes.Clear();
        Classes.Add("Vše");
        foreach (var c in classes.OrderBy(c => c)) Classes.Add(c.ToString());
    }

    // Načte seznam pozic pro filtr pracovníka
    public async Task LoadPositionsAsync()
    {
        var positions = await _db.Pozice.Select(p => p.Nazev).ToListAsync();
        Positions.Clear();
        Positions.Add("Vše");
        foreach (var p in positions.OrderBy(p => p)) Positions.Add(p);
    }

    // Načte kategorie jídel pro filtr
    public async Task LoadFoodCategoriesAsync()
    {
        var foodCategories = await _db.Jidlo.Select(j => j.Kategorie).Distinct().ToListAsync();
        FoodCategories.Clear();
        FoodCategories.Add("Vše");
        foreach (var c in foodCategories.OrderBy(c => c)) FoodCategories.Add(c);
    }

    // Načte typy (Alergie/Dietní omezení) pro přehled
    public async Task LoadDietTypesAsync()
    {
        var dietTypes = await _db.DietniOmezeni.Select(d => d.Nazev).Distinct().ToListAsync();
        DietTypes.Clear();
        DietTypes.Add("Vše");
        foreach (var d in dietTypes.OrderBy(d => d)) DietTypes.Add(d);
    }

    public async Task LoadMenuTypesAsync()
    {
        var menuTypes = await _db.Menu.Select(m => m.Nazev).Distinct().ToListAsync();
        MenuCategories.Clear();
        MenuCategories.Add("Vše");
        foreach (var m in MenuCategories.OrderBy(m => m)) MenuCategories.Add(m);
    }

    public async Task LoadObjednavkyStavAsync()
    {
        var objStav = await _db.Stav.Select(s => s.Nazev).Distinct().OrderBy(s => s).ToListAsync();
        ObjednavkyStav.Clear();
        ObjednavkyStav.Add("Vše");
        foreach (var s in objStav) ObjednavkyStav.Add(s);
    }

    // Pomocná metoda: odhadne ID z entity pro textový přehled
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