using System.Globalization;
using System.Windows.Data;
using System;

namespace SkolniJidelna.Helpers
{
    // Invertuje bool pro vazby v XAML (true -> false, false -> true)
    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            // pokud je null a cílový typ nullable bool, vrať true (alternativa: Binding.DoNothing)
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