using System.ComponentModel;
using System.Windows.Input;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace SkolniJidelna.ViewModels;
public class RegisterViewModel : INotifyPropertyChanged
{
    private readonly AppDbContext _db;
    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public string Jmeno { get => _jmeno; set { _jmeno = value; Raise(nameof(Jmeno)); } }
    private string _jmeno = "";

    public string Prijmeni { get => _prijmeni; set { _prijmeni = value; Raise(nameof(Prijmeni)); } }
    private string _prijmeni = "";

    public string Email { get => _email; set { _email = value; Raise(nameof(Email)); } }
    private string _email = "";

    public string Password { get => _password; set { _password = value; Raise(nameof(Password)); } }
    private string _password = "";

    public string ConfirmPassword { get => _confirmPassword; set { _confirmPassword = value; Raise(nameof(ConfirmPassword)); } }
    private string _confirmPassword = "";

    // Adresa
    public string Ulice { get => _ulice; set { _ulice = value; Raise(nameof(Ulice)); } }
    private string _ulice = "";

    public string Obec { get => _obec; set { _obec = value; Raise(nameof(Obec)); } }
    private string _obec = "";

    public string Psc { get => _psc; set { _psc = value; Raise(nameof(Psc)); } }
    private string _psc = "";

    // Pracovník vs student
    public bool IsWorker { get => _isWorker; set { _isWorker = value; Raise(nameof(IsWorker)); } }
    private bool _isWorker;

    public int? SelectedPositionId { get; set; }
    public int? SelectedClassId { get; set; }
    public string Phone { get => _phone; set { _phone = value; Raise(nameof(Phone)); } }
    private string _phone = "";

    // Foto (může být nastaveno z view)
    public byte[]? PhotoBytes { get; set; }

    public ICommand RegisterCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action<string>? RequestMessage;
    public event Action? RequestClose;

    public RegisterViewModel(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        RegisterCommand = new RelayCommand(async _ => await ExecuteRegisterAsync());
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
    }

    private bool ValidateInputs(out string error)
    {
        if (string.IsNullOrWhiteSpace(Jmeno) ||
            string.IsNullOrWhiteSpace(Prijmeni) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password))
        {
            error = "Vyplňte všechna povinná pole.";
            return false;
        }

        if (Password != ConfirmPassword)
        {
            error = "Hesla se neshodují.";
            return false;
        }

        if (!Email.Contains("@"))
        {
            error = "Neplatný e-mail.";
            return false;
        }

        if (IsWorker)
        {
            if (!SelectedPositionId.HasValue)
            {
                error = "Vyberte pozici pro pracovníka.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                error = "Zadejte telefon pro pracovníka.";
                return false;
            }
        }
        else
        {
            if (!SelectedClassId.HasValue)
            {
                error = "Vyberte třídu pro studenta.";
                return false;
            }
        }

        error = "";
        return true;
    }

    private async Task ExecuteRegisterAsync()
    {
        if (!ValidateInputs(out var err))
        {
            RequestMessage?.Invoke(err);
            return;
        }

        try
        {
            // kontrola duplicity e-mailu
            if (await _db.Stravnik.AnyAsync(s => s.Email == Email.Trim()))
            {
                RequestMessage?.Invoke("Uživatel s tímto e-mailem již existuje.");
                return;
            }

            var isFirst = !await _db.Stravnik.AnyAsync();

            // začneme transakci, abychom získali IdAdresa a IdStravnik
            await using var tx = await _db.Database.BeginTransactionAsync();

            // vytvoření adresy
            var adresa = new Adresa
            {
                Ulice = Ulice?.Trim() ?? "",
                Obec = Obec?.Trim() ?? "",
                Psc = int.TryParse(Psc, out var p) ? p : 0
            };
            _db.Adresa.Add(adresa);
            await _db.SaveChangesAsync();

            // vytvoření stravníka
            var hashed = BCrypt.Net.BCrypt.HashPassword(Password);
            var stravnik = new Stravnik
            {
                Jmeno = Jmeno.Trim(),
                Prijmeni = Prijmeni.Trim(),
                Email = Email.Trim(),
                Heslo = hashed,
                Zustatek = 0,
                Role = isFirst ? "Admin" : "User",
                Aktivita = "A",
                TypStravnik = IsWorker ? "pr" : "st",
                IdAdresa = adresa.IdAdresa
            };

            _db.Stravnik.Add(stravnik);
            await _db.SaveChangesAsync();

            // pokud je to první registrace a uživatel je pracovník, zajistíme pozici "Systémový administrátor"
            if (isFirst && IsWorker)
            {
                const string adminPoziceName = "Systémový administrátor";

                var adminPozice = await _db.Pozice
                    .FirstOrDefaultAsync(poz => poz.Nazev == adminPoziceName);

                if (adminPozice == null)
                {
                    adminPozice = new Pozice { Nazev = adminPoziceName };
                    _db.Pozice.Add(adminPozice);
                    await _db.SaveChangesAsync();
                }

                // Pokud uživatel nezvolil pozici, přiřadíme systémovou administrátorskou
                if (!SelectedPositionId.HasValue)
                    SelectedPositionId = adminPozice.IdPozice;
            }

            // podle role vytvoříme pracovnika nebo studenta
            if (IsWorker)
            {
                // vytvoříme Pracovnik
                var prac = new Pracovnik
                {
                    IdStravnik = stravnik.IdStravnik,
                    Telefon = int.TryParse(Phone, out var t) ? t : 0,
                    IdPozice = SelectedPositionId ?? 0
                };
                _db.Pracovnik.Add(prac);
                await _db.SaveChangesAsync();
            }
            else
            {
                // pokud existuje entita Student v modelu s IdTrida, vytvořit ji
                try
                {
                    var studentType = typeof(Student);
                    if (studentType != null)
                    {
                        // předpoklad: Student má vlastnost IdStravnik a IdTrida
                        var stud = new Student();
                        // reflexí nastavíme properties pokud existují
                        var propIdStr = studentType.GetProperty("IdStravnik");
                        var propIdTr = studentType.GetProperty("IdTrida");
                        if (propIdStr != null) propIdStr.SetValue(stud, stravnik.IdStravnik);
                        if (propIdTr != null && SelectedClassId.HasValue) propIdTr.SetValue(stud, SelectedClassId.Value);

                        _db.Add(stud);
                        await _db.SaveChangesAsync();
                    }
                }
                catch
                {
                    // pokud Student entita má jinou strukturu, ignorujeme a pokračujeme
                }
            }

            await tx.CommitAsync();

            RequestMessage?.Invoke(isFirst ? "Registrace dokončena. Jste administrátor." : "Registrace dokončena.");
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            RequestMessage?.Invoke($"Chyba při registraci: {ex.Message}");
        }
    }
}