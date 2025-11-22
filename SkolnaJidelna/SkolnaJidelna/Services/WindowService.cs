using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SkolniJidelna.Services
{
    public class WindowService : IWindowService
    {
        public void ShowAdminProfile()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = new AdminProfileWindow();
                w.Show();
            });
        }

        public void ShowUserProfile(string email, bool isPracovnik)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var w = new UserProfileWindow(email, isPracovnik);
                w.Show();
            });
        }
    }
}
