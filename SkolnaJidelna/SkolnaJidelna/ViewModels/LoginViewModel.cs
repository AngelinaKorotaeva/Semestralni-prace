using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using SkolniJidelna.Data;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace SkolniJidelna.ViewModels;
public class LoginViewModel : INotifyPropertyChanged
{
    private readonly AppDbContext _db;

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string Username { get => _username; set { _username = value; Raise(nameof(Username)); } }
    private string _username = "";

    // Password se bude vázat pomocí PasswordBoxAssistant
    public string Password { get => _password; set { _password = value; Raise(nameof(Password)); } }
    private string _password = "";

    public ICommand LoginCommand { get; }
    public ICommand RegisterCommand { get; }

    // Události, které view zachytí
    public event Action<string, bool>? LoginSucceeded; // email, isPracovnik
    public event Action<string>? LoginFailed; // zpráva
    public event Action? RegisterRequested;

    public LoginViewModel(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
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
            var user = await _db.Stravnik.FirstOrDefaultAsync(s => s.Email == Username);
            if (user == null)
            {
                LoginFailed?.Invoke("Uživatel neexistuje.");
                return;
            }

            // bezpečné ověření hesla s opravou prefixu a zachycením chyb
            bool verified = VerifyPasswordSafe(Password, user.Heslo);

            // ---- volitelná migrace: pokud v DB je plaintext (NEDOPORUČUJE SE v produkci)
            // if (!verified && user.Heslo != null && user.Heslo == Password)
            // {
            //     // Zde můžete vygenerovat bcrypt hash a uložit ho zpět (migrace)
            //     user.Heslo = BCrypt.Net.BCrypt.HashPassword(Password);
            //     await _db.SaveChangesAsync();
            //     verified = true;
            // }

            if (!verified)
            {
                LoginFailed?.Invoke("Nesprávné uživatelské jméno nebo heslo.");
                return;
            }

            bool isPracovnik = user.TypStravnik?.ToLower() == "pr";
            LoginSucceeded?.Invoke(user.Email, isPracovnik);
        }
        catch (Exception ex)
        {
            // bezpečné hlášení
            LoginFailed?.Invoke($"Chyba při přihlášení: {ex.Message}");
        }
    }

    // Bezpečné ověření bcrypt hesla: zkouší standardní Verify, při chybě opraví prefixy $2y$ -> $2a$/$2b$ a zkusí znovu.
    private bool VerifyPasswordSafe(string plainPassword, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        try
        {
            // Standardní ověření
            if (BCrypt.Net.BCrypt.Verify(plainPassword, storedHash))
                return true;
        }
        catch (Exception ex) when (ex is System.ArgumentException || ex is FormatException || ex is BCrypt.Net.SaltParseException)
        {
            // pokračujeme do oprav prefixu níže
        }

        // Některé implementace Oracle/externích služeb ukládají $2y$ prefix — starší knihovny očekávají $2a$ nebo $2b$.
        // Zkusíme možné náhrady (bez úprav originálu v DB).
        try
        {
            if (storedHash.StartsWith("$2y$"))
            {
                var alt = "$2a$" + storedHash.Substring(4);
                if (BCrypt.Net.BCrypt.Verify(plainPassword, alt)) return true;

                alt = "$2b$" + storedHash.Substring(4);
                if (BCrypt.Net.BCrypt.Verify(plainPassword, alt)) return true;
            }

            // Pokud hash používá jiné prefixy, přidávat další opravy zde
        }
        catch
        {
            // ignorovat — vrátíme false níže
        }

        // Nepovedlo se ověřit
        return false;
    }
}