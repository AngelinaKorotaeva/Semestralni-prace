using System;

namespace SkolniJidelna.Services
{
    // Rozhraní služby pro otevírání oken – oddělení UI navigace od ViewModelů
    public interface IWindowService
    {
        // Otevře okno profilu administrátora podle jeho emailu.
        void ShowAdminProfile(string adminEmail);
        // Otevře okno profilu uživatele podle emailu; isPracovnik určuje variantu.
        void ShowUserProfile(string email, bool isPracovnik);
    }
}
