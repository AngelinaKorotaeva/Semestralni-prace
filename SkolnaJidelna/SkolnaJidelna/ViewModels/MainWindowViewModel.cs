using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using SkolniJidelna.Data;
using SkolniJidelna.Services;
using System.IO;
using System.Windows.Xps.Serialization;
using System.Diagnostics;
using SkolniJidelna.Helpers;

namespace SkolniJidelna.ViewModels
{
    // ViewModel hlavního okna – přihlašovací logika (MVVM)
    // Ověřuje vstupy, kontroluje heslo (BCrypt), vyhodnocuje roli a otevírá příslušná okna přes IWindowService
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        private readonly IWindowService _windowService;

        public event PropertyChangedEventHandler? PropertyChanged;
        void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // Přihlašovací údaje z UI
        private string _username = "";
        public string Username { get => _username; set { if (_username == value) return; _username = value; Raise(nameof(Username)); } }

        private string _password = "";
        public string Password { get => _password; set { if (_password == value) return; _password = value; Raise(nameof(Password)); } }

        // Příkazy pro tlačítka
        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        // Události pro View (úspěch/neúspěch přihlášení, žádost o registraci)
        public event Action<string, bool, bool>? LoginSucceeded; // email, isPracovnik, isAdmin
        public event Action<string>? LoginFailed;
        public event Action? RegisterRequested;

        public MainWindowViewModel(AppDbContext db, IWindowService windowService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            LoginCommand = new RelayCommand(async _ => await ExecuteLoginAsync());
            RegisterCommand = new RelayCommand(_ => RegisterRequested?.Invoke());
        }

        // Jednoduchá validace e‑mailu
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email.Trim(), pattern);
        }

        // Hlavní login rutina: načte uživatele z DB, ověří heslo, vyhodnotí roli a přesměruje do příslušného okna
        private async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Username) || Username == "Uživatelské jméno" || string.IsNullOrWhiteSpace(Password))
            {
                LoginFailed?.Invoke("Prosím vyplňte všechny údaje.");
                return;
            }

            if (!IsValidEmail(Username))
            {
                LoginFailed?.Invoke("Zadejte platný e-mail.");
                return;
            }

            try
            {
                using var ctx = new AppDbContext();
                var user = await ctx.VStravnikLogin.FirstOrDefaultAsync(x => x.Email == Username);

                if (user == null)
                {
                    LoginFailed?.Invoke("Uživatel neexistuje.");
                    return;
                }

                // Ověření hesla (včetně náhražky $2y$ -> $2a$/$2b$)
                var verified = VerifyPasswordSafe(Password, user.Heslo);
                if (!verified)
                {
                    LoginFailed?.Invoke("Nesprávné uživatelské jméno nebo heslo.");
                    return;
                }

                // Debug logy do Debug Output a souboru
                Debug.WriteLine($"Login attempt - Email={user.Email}, RoleRaw='{user.Role}', TypStravnik='{user.TypStravnik}'");
                try
                {
                    File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "login-debug.log"),
                        $"Login attempt - Email={user.Email}, RoleRaw='{user.Role}', TypStravnik='{user.TypStravnik}', Time={DateTime.Now:o}{Environment.NewLine}");
                }
                catch { }

                var roleNorm = user.Role?.Trim();
                var typeNorm = user.TypStravnik?.Trim();

                bool isPracovnik = string.Equals(typeNorm, "pr", StringComparison.OrdinalIgnoreCase);
                bool isAdmin = string.Equals(roleNorm, "ADMIN", StringComparison.OrdinalIgnoreCase);

                if (!isAdmin && !string.IsNullOrEmpty(roleNorm) && !string.Equals(roleNorm, "USER", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "login-role-debug.log"),
                            $"User={user.Email}, RoleRaw='{user.Role}', RoleNorm='{roleNorm}', Time={DateTime.Now:o}{Environment.NewLine}");
                    }
                    catch { }
                }

                // Navigace přes službu (MVVM-friendly)
                if (isAdmin)
                {
                    _windowService.ShowAdminProfile(user.Email);
                }
                else
                {
                    _windowService.ShowUserProfile(user.Email, isPracovnik);
                }

                // Notifikace pro View – může zavřít přihlašovací okno
                LoginSucceeded?.Invoke(user.Email, isPracovnik, isAdmin);
            }
            catch (Exception ex)
            {
                LoginFailed?.Invoke($"Chyba při přihlášení: {ex.Message}");
            }
        }

        // Robustní ověření bcrypt (řeší varianty $2y$)
        private bool VerifyPasswordSafe(string plainPassword, string? storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            try
            {
                if (BCrypt.Net.BCrypt.Verify(plainPassword, storedHash))
                    return true;
            }
            catch (Exception) { }

            try
            {
                if (storedHash.StartsWith("$2y$"))
                {
                    var alt = "$2a$" + storedHash.Substring(4);
                    if (BCrypt.Net.BCrypt.Verify(plainPassword, alt)) return true;

                    alt = "$2b$" + storedHash.Substring(4);
                    if (BCrypt.Net.BCrypt.Verify(plainPassword, alt)) return true;
                }
            }
            catch { }

            return false;
        }
    }
}