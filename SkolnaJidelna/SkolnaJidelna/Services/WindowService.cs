using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SkolniJidelna.Services
{
    /// <summary>
    /// Implementace `IWindowService` – otevírá WPF okna bezpečně na UI vlákně přes `Dispatcher`.
    /// Zabraňuje přímé závislosti ViewModelu na UI a usnadňuje testy/mocking.
    /// </summary>
    public class WindowService : IWindowService
    {
        /// <summary>
        /// Otevře okno profilu administrátora; volání je marshálované na UI vlákno.
        /// </summary>
        public void ShowAdminProfile(string adminEmail)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = new AdminProfileWindow(adminEmail);
                w.Show();
            });
        }

        /// <summary>
        /// Otevře okno profilu uživatele; volání je marshálované na UI vlákno.
        /// Parametr `isPracovnik` je ponechán pro případnou logiku odlišení UI.
        /// </summary>
        public void ShowUserProfile(string email, bool isPracovnik)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = new UserProfileWindow(email);
                w.Show();
            });
        }
    }
}
