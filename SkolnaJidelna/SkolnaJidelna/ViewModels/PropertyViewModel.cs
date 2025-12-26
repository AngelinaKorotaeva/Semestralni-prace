using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels;
// ViewModel jedné vlastnosti entity pro admin UI
// Udržuje název, typ, hodnotu a vyvolá změny zpět do entity pomocí callbacku
public class PropertyViewModel : INotifyPropertyChanged
{
    public string Name { get; }
    public Type PropertyType { get; }
    private object? _value;
    private readonly Action<object?> _onChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    // Typ editoru v UI (Default/Combo/MultiSelect)
    public string EditorType { get; set; } = "Default";
    // Zdroj položek pro Combo/MultiSelect editory
    public IEnumerable ItemsSource { get; set; }

    public PropertyViewModel(string name, Type propertyType, object? value, Action<object?> onChanged)
    {
        Name = name;
        PropertyType = propertyType;
        _value = value;
        _onChanged = onChanged;
    }

    public object? Value
    {
        get => _value;
        set
        {
            if (!Equals(_value, value))
            {
                _value = value;
                // Notifikace pro binding a zápis změny zpět do entity
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                _onChanged?.Invoke(value);
            }
        }
    }
}

// Položka pro multi‑výběr (checkbox list) s názvem a příznakem vybranosti
public class SelectableItem
{
    public object Item { get; }
    public string Nazev { get; }
    public bool IsSelected { get; set; }

    public SelectableItem(object item, bool isSelected)
    {
        Item = item;
        Nazev = item is Alergie a ? a.Nazev : item is DietniOmezeni d ? d.Nazev : item.ToString();
        IsSelected = isSelected;
    }
}