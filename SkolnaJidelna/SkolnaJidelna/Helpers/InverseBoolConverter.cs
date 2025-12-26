using System.Globalization;
using System.Windows.Data;
using System;

namespace SkolniJidelna.Helpers
{
    // Konvertor, který invertuje bool hodnotu pro XAML bindingy (true -> false, false -> true)
    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            // Pokud je null a cílový typ je bool/bool?, vrátí true (lze upravit dle potřeby)
            if (value == null && (targetType == typeof(bool) || targetType == typeof(bool?))) return true;
            return Binding.DoNothing;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return Binding.DoNothing;
        }
    }
}