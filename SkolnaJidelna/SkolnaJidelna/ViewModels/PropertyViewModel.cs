using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels;
public class PropertyViewModel : INotifyPropertyChanged
{
    public string Name { get; }
    public Type PropertyType { get; }
    private object? _value;
    private readonly Action<object?> _onChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public string EditorType { get; set; } = "Default";
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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                _onChanged?.Invoke(value);
            }
        }
    }
}

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