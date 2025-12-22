using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Services
{
    public interface IWindowService
    {
        // Otevře okno profilu administrátora podle jeho emailu.
        void ShowAdminProfile(string adminEmail);
        // Otevře okno profilu uživatele podle emailu; isPracovnik určuje, zda zobrazit variantu pro pracovníka nebo studenta.
        void ShowUserProfile(string email, bool isPracovnik);
    }
}
