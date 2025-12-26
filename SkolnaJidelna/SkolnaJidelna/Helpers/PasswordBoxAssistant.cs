using System.Windows;
using System.Windows.Controls;

namespace SkolniJidelna.Helpers;
// Pomocná třída pro dvoucestné svázání hesla z `PasswordBox` do ViewModelu
// Řeší absenci `Password` dependency property na PasswordBoxu pomocí připojených vlastností
public static class PasswordBoxAssistant
{
    // Aktivuje/deaktivuje vazbu na PasswordBox
    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached("BindPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false, OnBindPasswordChanged));

    // Vázaná hodnota hesla (string) – zapisuje se z/do VM
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxAssistant), new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

    // Interní příznak pro zamezení rekurzivního vyvolání
    private static readonly DependencyProperty UpdatingPasswordProperty =
        DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false));


    public static void SetBindPassword(DependencyObject dp, bool value) => dp.SetValue(BindPasswordProperty, value);
    public static bool GetBindPassword(DependencyObject dp) => (bool)dp.GetValue(BindPasswordProperty);


    public static void SetBoundPassword(DependencyObject dp, string value) => dp.SetValue(BoundPasswordProperty, value);
    public static string GetBoundPassword(DependencyObject dp) => (string)dp.GetValue(BoundPasswordProperty);


    private static bool GetUpdatingPassword(DependencyObject dp) => (bool)dp.GetValue(UpdatingPasswordProperty);
    private static void SetUpdatingPassword(DependencyObject dp, bool value) => dp.SetValue(UpdatingPasswordProperty, value);

    // Připojí/odpojí handler podle BindPassword
    private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        if (dp is not PasswordBox box) return;
        if ((bool)e.NewValue) box.PasswordChanged += PasswordChanged; else box.PasswordChanged -= PasswordChanged;
    }

    // Změna vázané hodnoty -> nastaví PasswordBox.Password bez vyvolání smyčky
    private static void OnBoundPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        if (dp is not PasswordBox box) return;
        if (GetUpdatingPassword(box)) return;
        box.PasswordChanged -= PasswordChanged;
        box.Password = (string?)e.NewValue ?? string.Empty;
        box.PasswordChanged += PasswordChanged;
    }

    // Změna v UI -> uloží heslo do BoundPassword (VM)
    private static void PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox box) return;
        SetUpdatingPassword(box, true);
        SetBoundPassword(box, box.Password);
        SetUpdatingPassword(box, false);
    }
}