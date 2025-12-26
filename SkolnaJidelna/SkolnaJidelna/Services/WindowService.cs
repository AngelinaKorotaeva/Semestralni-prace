using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SkolniJidelna.Services
{
    // Implementace `IWindowService` – otevírá WPF okna na UI vlákně přes Dispatcher
    public class WindowService : IWindowService
    {
        public void ShowAdminProfile(string adminEmail)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = new AdminProfileWindow(adminEmail);
                w.Show();
            });
        }

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
