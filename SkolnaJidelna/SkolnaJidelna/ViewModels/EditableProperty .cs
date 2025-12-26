using System;
using System.ComponentModel;
using System.Globalization;

namespace SkolniJidelna.ViewModels.SkolniJidelna.ViewModels
{
    // Reprezentuje jednu editovatelnou vlastnost (název, typ, hodnota)
    // Poskytuje i textovou reprezentaci pro binding v UI (StringValue) s konverzí na cílový typ
    public class EditableProperty : INotifyPropertyChanged
    {
        public string Name { get; }
        public Type PropertyType { get; }
        object? _value;

        // Hodnota v původním typu (propaguje notifikace i pro StringValue)
        public object? Value { get => _value; set { if (_value == value) return; _value = value; OnPropertyChanged(nameof(Value)); OnPropertyChanged(nameof(StringValue)); } }

        // Textová hodnota pro editaci; při nastavení se pokusí převést na PropertyType
        public string? StringValue
        {
            get => Value?.ToString();
            set
            {
                if (value == StringValue) return;
                try { Value = ConvertToType(value, PropertyType); }
                catch { Value = value; }
                OnPropertyChanged(nameof(StringValue));
            }
        }

        public EditableProperty(string name, Type type, object? value)
        {
            Name = name; PropertyType = type; Value = value;
        }

        // Pomocná konverze z textu na cílový typ (podpora Nullable<>)
        static object? ConvertToType(string? s, Type target)
        {
            if (s == null) return null;
            var t = Nullable.GetUnderlyingType(target) ?? target;
            if (t == typeof(string)) return s;
            if (t == typeof(bool)) return bool.Parse(s);
            if (t == typeof(int)) return int.Parse(s, CultureInfo.InvariantCulture);
            if (t == typeof(double)) return double.Parse(s, CultureInfo.InvariantCulture);
            if (t == typeof(DateTime)) return DateTime.Parse(s, CultureInfo.InvariantCulture);
            return System.Convert.ChangeType(s, t, CultureInfo.InvariantCulture);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
