using System.Windows;
using System.Windows.Controls;
using System;

namespace SkolniJidelna.ViewModels;
// Selektor šablon editorů pro vlastnosti v admin UI
// Rozhoduje, jaký DataTemplate použít podle typu vlastnosti nebo požadovaného editoru (Combo/MultiSelect)
public class PropertyEditorSelector : DataTemplateSelector
{
    public DataTemplate? StringTemplate { get; set; }
    public DataTemplate? BoolTemplate { get; set; }
    public DataTemplate? NumberTemplate { get; set; }
    public DataTemplate? DateTemplate { get; set; }
    public DataTemplate? ComboTemplate { get; set; }
    public DataTemplate? MultiSelectTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not PropertyViewModel p) return base.SelectTemplate(item, container);

        // Přímé určení editoru podle EditorType
        if (p.EditorType == "Combo") return ComboTemplate ?? base.SelectTemplate(item, container);
        if (p.EditorType == "MultiSelect") return MultiSelectTemplate ?? base.SelectTemplate(item, container);

        // Jinak rozhodnutí podle skutečného typu vlastnosti (včetně Nullable<>)
        var t = p.PropertyType;
        var underlying = Nullable.GetUnderlyingType(t) ?? t;

        if (underlying == typeof(bool)) return BoolTemplate ?? base.SelectTemplate(item, container);
        if (underlying == typeof(DateTime)) return DateTemplate ?? base.SelectTemplate(item, container);
        if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(float)
            || underlying == typeof(double) || underlying == typeof(decimal))
            return NumberTemplate ?? base.SelectTemplate(item, container);

        // Fallback na textový editor
        return StringTemplate ?? base.SelectTemplate(item, container);
    }
}