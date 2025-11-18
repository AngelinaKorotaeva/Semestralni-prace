using System;
using System.ComponentModel;

namespace SkolniJidelna.ViewModels;
public class PropertyViewModel : INotifyPropertyChanged
{
    public string Name { get; }
    public Type PropertyType { get; }
    private object? _value;
    private readonly Action<object?> _onChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

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