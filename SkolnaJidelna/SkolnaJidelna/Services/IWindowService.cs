using System;

namespace SkolniJidelna.Services
{
    /// <summary>
    /// Služba pro otevírání aplikačních oken – abstrahuje navigaci mimo ViewModel (pro DI/testy).
    /// </summary>
    public interface IWindowService
    {
        /// <summary>
        /// Otevře okno profilu administrátora pro zadaný e‑mail.
        /// </summary>
        void ShowAdminProfile(string adminEmail);
        /// <summary>
        /// Otevře okno profilu uživatele; parametr `isPracovnik` může měnit inicializaci UI.
        /// </summary>
        void ShowUserProfile(string email, bool isPracovnik);
    }
}
