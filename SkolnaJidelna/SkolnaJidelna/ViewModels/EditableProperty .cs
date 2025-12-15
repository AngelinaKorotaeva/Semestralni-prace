using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    namespace SkolniJidelna.ViewModels
    {
        public class EditableProperty : INotifyPropertyChanged
        {
            public string Name { get; }
            public Type PropertyType { get; }
            object? _value;
            public object? Value { get => _value; set { if (_value == value) return; _value = value; OnPropertyChanged(nameof(Value)); OnPropertyChanged(nameof(StringValue)); } }

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
}
