using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using SkolniJidelna.Data;
using SkolniJidelna.Services;

namespace SkolniJidelna.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        private readonly IWindowService _windowService;

        public event PropertyChangedEventHandler? PropertyChanged;
        void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _username = "";
        public string Username { get => _username; set { if (_username == value) return; _username = value; Raise(nameof(Username)); } }

        private string _password = "";
        public string Password { get => _password; set { if (_password == value) return; _password = value; Raise(nameof(Password)); } }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        // email, isPracovnik, isAdmin
        public event Action<string, bool, bool>? LoginSucceeded;
        public event Action<string>? LoginFailed;
        public event Action? RegisterRequested;

        public MainWindowViewModel(AppDbContext db, IWindowService windowService)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            LoginCommand = new RelayCommand(async _ => await ExecuteLoginAsync());
            RegisterCommand = new RelayCommand(_ => RegisterRequested?.Invoke());
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email.Trim(), pattern);
        }

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

                // ověření hesla
                var verified = VerifyPasswordSafe(Password, user.Heslo);
                if (!verified)
                {
                    LoginFailed?.Invoke("Nesprávné uživatelské jméno nebo heslo.");
                    return;
                }

                bool isPracovnik = string.Equals(user.TypStravnik, "pr", StringComparison.OrdinalIgnoreCase);
                bool isAdmin = string.Equals(user.Role, "ADMIN", StringComparison.OrdinalIgnoreCase);

                // Навигация через сервис (MVVM-friendly)
                if (isAdmin)
                    _windowService.ShowAdminProfile();
                else
                    _windowService.ShowUserProfile(user.Email, isPracovnik);

                // уведомление view, чтобы оно могло закрыть окно
                LoginSucceeded?.Invoke(user.Email, isPracovnik, isAdmin);
            }
            catch (Exception ex)
            {
                LoginFailed?.Invoke($"Chyba při přihlášení: {ex.Message}");
            }
        }

        // Robust bcrypt verification (tries $2y$ fixes)
        private bool VerifyPasswordSafe(string plainPassword, string? storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            try
            {
                if (BCrypt.Net.BCrypt.Verify(plainPassword, storedHash))
                    return true;
            }
            catch (Exception) { /* fallthrough */ }

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
            catch { /* ignore */ }

            return false;
        }
    }
}