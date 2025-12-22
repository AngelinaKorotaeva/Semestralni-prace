using System.Windows;
using System.Windows.Controls;

namespace SkolniJidelna.Helpers;
public static class PasswordBoxAssistant
{
    // Registruje připojení k PasswordBoxu a hlídá, zda se má heslo svazovat s ViewModelem.
    public static readonly DependencyProperty BindPasswordProperty =
        DependencyProperty.RegisterAttached("BindPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false, OnBindPasswordChanged));

    // Skutečná vázaná hodnota hesla (string) – z/do ViewModelu.
    public static readonly DependencyProperty BoundPasswordProperty =
        DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxAssistant), new PropertyMetadata(string.Empty, OnBoundPasswordChanged));

    // Interní příznak, že probíhá update z kódu, aby nevznikla smyčka.
    private static readonly DependencyProperty UpdatingPasswordProperty =
        DependencyProperty.RegisterAttached("UpdatingPassword", typeof(bool), typeof(PasswordBoxAssistant), new PropertyMetadata(false));


    // BindPassword

    public static void SetBindPassword(DependencyObject dp, bool value) => dp.SetValue(BindPasswordProperty, value);
    public static bool GetBindPassword(DependencyObject dp) => (bool)dp.GetValue(BindPasswordProperty);


    // BoundPassword: zapisuje hodnotu BoundPassword na konkrétní PasswordBox (pro binding z VM).
    public static void SetBoundPassword(DependencyObject dp, string value) => dp.SetValue(BoundPasswordProperty, value);
    public static string GetBoundPassword(DependencyObject dp) => (string)dp.GetValue(BoundPasswordProperty);


    // Getter/setter interního příznaku – používá se jen uvnitř pro blokaci rekurze.
    private static bool GetUpdatingPassword(DependencyObject dp) => (bool)dp.GetValue(UpdatingPasswordProperty);
    private static void SetUpdatingPassword(DependencyObject dp, bool value) => dp.SetValue(UpdatingPasswordProperty, value);

    // OnBindPasswordChanged: připojí/odpojí handler PasswordChanged podle toho, zda je BindPassword=true.
    private static void OnBindPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        if (dp is not PasswordBox box) return;

        if ((bool)e.NewValue)
        {
            box.PasswordChanged += PasswordChanged;
        }
        else
        {
            box.PasswordChanged -= PasswordChanged;
        }
    }

    // OnBoundPasswordChanged: když se změní vázaný string z VM, přepíše PasswordBox.Password (bez vyvolání smyčky).
    private static void OnBoundPasswordChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
    {
        if (dp is not PasswordBox box) return;
        if (GetUpdatingPassword(box)) return;

        box.PasswordChanged -= PasswordChanged;
        box.Password = (string?)e.NewValue ?? string.Empty;
        box.PasswordChanged += PasswordChanged;
    }

    // PasswordChanged: reakce na změnu hesla v UI – zapíše nové heslo do vázaného property BoundPassword.
    private static void PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is not PasswordBox box) return;
        SetUpdatingPassword(box, true);
        SetBoundPassword(box, box.Password);
        SetUpdatingPassword(box, false);
    }
}