using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SkolniJidelna.ViewModels;
public class AdminViewModel : INotifyPropertyChanged
{
    private readonly AppDbContext _db;
    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    public ObservableCollection<EntityTypeItem> EntityTypes { get; } = new();
    public ObservableCollection<EntityItem> Items { get; } = new();

    private EntityTypeItem? _selectedEntityType;
    public EntityTypeItem? SelectedEntityType
    {
        get => _selectedEntityType;
        set
        {
            if (_selectedEntityType == value) return;
            _selectedEntityType = value;
            Raise(nameof(SelectedEntityType));
            (RefreshCommand as RelayCommand)?.RaiseCanExecuteChanged();
            _ = LoadItemsAsync();
        }
    }

    private EntityItem? _selectedItem;
    public EntityItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem == value) return;
            _selectedItem = value;
            Raise(nameof(SelectedItem));
            BuildPropertyEditors();
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    public ObservableCollection<PropertyViewModel> Properties { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CloseCommand { get; }

    public event Action? CloseRequested;

    public AdminViewModel(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => SelectedItem != null);
        RefreshCommand = new RelayCommand(async _ => await LoadItemsAsync(), _ => SelectedEntityType != null);
        CloseCommand = new RelayCommand(_ => CloseRequested?.Invoke());
    }

    public async Task InitializeAsync()
    {
        EntityTypes.Clear();
        var types = _db.Model.GetEntityTypes()
            .Select(e => e.ClrType)
            .Distinct()
            .OrderBy(t => t.Name);

        foreach (var t in types)
            EntityTypes.Add(new EntityTypeItem(t));

        SelectedEntityType = EntityTypes.FirstOrDefault();
        if (SelectedEntityType != null) await LoadItemsAsync();
    }

    private async Task LoadItemsAsync()
    {
        Items.Clear();
        Properties.Clear();
        if (SelectedEntityType == null) return;

        var type = SelectedEntityType.Type;

        // zavolat DbContext.Set<T>() přes reflection (kompilátor neřeší generika z runtime typu)
        var setMethod = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)
                        ?? throw new InvalidOperationException("Nelze nalézt metodu DbContext.Set()");
        var genericSet = setMethod.MakeGenericMethod(type);
        var dbSet = genericSet.Invoke(_db, null);
        if (dbSet == null) return;

        // Enumerace na pozadí (EF vykoná dotaz při enumeraci)
        var list = await Task.Run(() =>
        {
            var result = new List<object>();
            if (dbSet is IEnumerable en)
            {
                foreach (var item in en)
                {
                    if (item != null) result.Add(item);
                }
            }
            return result;
        });

        foreach (var e in list) Items.Add(new EntityItem(e, BuildSummary(e)));
    }

    private string BuildSummary(object entity)
    {
        var t = entity.GetType();
        string[] prefer = { "Jmeno", "Name", "Email", "Id", "IdStravnik", "Nazev" };
        foreach (var p in prefer)
        {
            var pi = t.GetProperty(p, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (pi != null)
            {
                var v = pi.GetValue(entity);
                if (v != null) return $"{t.Name} — {v}";
            }
        }

        var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => IsSimple(p.PropertyType)).ToArray();
        var vals = props.Take(2).Select(p => p.GetValue(entity)?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        return vals.Length > 0 ? $"{t.Name} — {string.Join(" | ", vals)}" : t.Name;
    }

    private static bool IsSimple(Type t) =>
        t.IsPrimitive
        || t == typeof(string)
        || t == typeof(DateTime)
        || t == typeof(decimal)
        || t == typeof(double)
        || t == typeof(float)
        || t == typeof(bool)
        || (Nullable.GetUnderlyingType(t) != null && IsSimple(Nullable.GetUnderlyingType(t)!));

    private void BuildPropertyEditors()
    {
        Properties.Clear();
        if (SelectedItem?.Entity == null) return;
        var obj = SelectedItem.Entity;
        var t = obj.GetType();
        foreach (var pi in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!pi.CanRead || !pi.CanWrite) continue;
            var pt = pi.PropertyType;
            if (!IsSimple(pt)) continue;
            var val = pi.GetValue(obj);
            var pvm = new PropertyViewModel(pi.Name, pt, val, (newVal) =>
            {
                try { pi.SetValue(obj, Convert.ChangeType(newVal, Nullable.GetUnderlyingType(pt) ?? pt)); }
                catch { pi.SetValue(obj, newVal); }
            });
            Properties.Add(pvm);
        }
    }

    private async Task SaveAsync()
    {
        if (SelectedItem?.Entity == null) return;
        var entity = SelectedItem.Entity;
        var entry = _db.Entry(entity);
        if (entry.State == EntityState.Detached)
        {
            _db.Attach(entity);
            entry = _db.Entry(entity);
        }
        entry.State = EntityState.Modified;
        await _db.SaveChangesAsync();
        SelectedItem.Summary = BuildSummary(entity);
        Raise(nameof(Items));
    }
}

public record EntityTypeItem(Type Type)
{
    public string Name => Type.Name;
}

public class EntityItem : INotifyPropertyChanged
{
    public object Entity { get; }
    private string _summary;
    public string Summary { get => _summary; set { _summary = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary))); } }
    public EntityItem(object entity, string summary) { Entity = entity; _summary = summary; }
    public event PropertyChangedEventHandler? PropertyChanged;
}