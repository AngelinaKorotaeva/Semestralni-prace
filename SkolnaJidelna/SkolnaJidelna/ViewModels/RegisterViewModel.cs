using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using BCrypt.Net;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using SkolniJidelna.Services;

namespace SkolniJidelna.ViewModels
{
    public class RegisterViewModel : INotifyPropertyChanged
    {
        private string _firstName = string.Empty;
        private string _lastName = string.Empty;
        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _status = "st"; // interní reprezentace: "st" nebo "pr"
        private string _phone = string.Empty;
        private int? _positionId;
        private int? _classId;
        private string? _photoPath;
        private string _birthYear = string.Empty;

        private readonly IFileDialogService _fileDialogService;

        // Commands
        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectPhotoCommand { get; }

        // Positions for UI
        public ObservableCollection<Pozice> Positions { get; private set; } = new ObservableCollection<Pozice>();

        // Selected position id (bind to ComboBox.SelectedValue)
        public int? PositionId { get => _positionId; set { if (_positionId == value) return; _positionId = value; OnPropertyChanged(nameof(PositionId)); } }

        // Events pro host okna / UI
        public event Action<string>? RequestMessage;   // zobrazit zprávu v UI
        public event Action? RequestClose;             // zavřít okno

        // Callbacky pro další použití
        public event Action<string, bool, bool>? RegistrationSucceeded; // email, isPracovnik, isAdmin
        public event Action<string>? RegistrationFailed;
        public event Action? CancelRequested;

        public RegisterViewModel() : this(new FileDialogService()) { }

        // DI-friendly konstruktor
        public RegisterViewModel(IFileDialogService fileDialogService)
        {
            _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
            RegisterCommand = new RelayCommand(_ => Register());
            CancelCommand = new RelayCommand(_ => Cancel());
            SelectPhotoCommand = new RelayCommand(_ => SelectPhoto());

            IsWorker = true;

            LoadPositions();
        }

        private void LoadPositions()
        {
            using var ctx = new AppDbContext();
            if (ctx.Pozice == null)
                return;

            if (ctx.Pozice.Count() == 0)
            {
                var nextId = ctx.Pozice.Count() >0 ? ctx.Pozice.Max(p => p.IdPozice) +1 :1;
                var adminPoz = new Pozice { IdPozice = nextId, Nazev = "Systémový administrátor" };
                ctx.Pozice.Add(adminPoz);
                ctx.SaveChanges();

                PositionId = adminPoz.IdPozice;
            }

            var list = ctx.Pozice.OrderBy(p => p.IdPozice).ToList();
            Positions = new ObservableCollection<Pozice>(list);
            OnPropertyChanged(nameof(Positions));

            if (PositionId == null)
            {
                var first = list.FirstOrDefault();
                if (first != null) PositionId = first.IdPozice;
            }
        }

        // Vlastnosti pro binding
        public string FirstName { get => _firstName; set { if (_firstName == value) return; _firstName = value; OnPropertyChanged(nameof(FirstName)); } }
        public string LastName { get => _lastName; set { if (_lastName == value) return; _lastName = value; OnPropertyChanged(nameof(LastName)); } }
        public string Email { get => _email; set { if (_email == value) return; _email = value; OnPropertyChanged(nameof(Email)); } }

        // Hesla jsou vázána přes PasswordBoxAssistant
        public string Password { get => _password; set { if (_password == value) return; _password = value; OnPropertyChanged(nameof(Password)); } }
        public string ConfirmPassword { get => _confirmPassword; set { if (_confirmPassword == value) return; _confirmPassword = value; OnPropertyChanged(nameof(ConfirmPassword)); } }

        // Status string pro DB
        public string Status
        {
            get => _status;
            set
            {
                var newVal = string.IsNullOrWhiteSpace(value) ? "st" : value;
                if (_status == newVal) return;
                _status = newVal;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsWorker));
                OnPropertyChanged(nameof(IsStudent));
            }
        }

        // TwoWay vazba vyžaduje setter - synchronizuje Status
        public bool IsWorker
        {
            get => string.Equals(_status, "pr", StringComparison.OrdinalIgnoreCase);
            set
            {
                var newStatus = value ? "pr" : "st";
                if (_status == newStatus) return;
                _status = newStatus;
                OnPropertyChanged(nameof(IsWorker));
                OnPropertyChanged(nameof(IsStudent));
                OnPropertyChanged(nameof(Status));
            }
        }

        public bool IsStudent => !IsWorker;

        public string Phone { get => _phone; set { if (_phone == value) return; _phone = value; OnPropertyChanged(nameof(Phone)); } }
        public int? ClassId { get => _classId; set { if (_classId == value) return; _classId = value; OnPropertyChanged(nameof(ClassId)); } }

        // Photo
        public string? PhotoPath { get => _photoPath; set { if (_photoPath == value) return; _photoPath = value; OnPropertyChanged(nameof(PhotoPath)); } }

        // Birth year jako string (snadnější vazba do TextBoxu)
        public string BirthYear { get => _birthYear; set { if (_birthYear == value) return; _birthYear = value; OnPropertyChanged(nameof(BirthYear)); } }

        // Výběr fotky (přes službu)
        private void SelectPhoto()
        {
            try
            {
                var path = _fileDialogService.OpenFileDialog();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    PhotoPath = path;
                }
            }
            catch (Exception ex)
            {
                RequestMessage?.Invoke("Chyba při výběru fotky: " + ex.Message);
            }
        }

        // Zrušení registrace
        public void Cancel()
        {
            CancelRequested?.Invoke();
            RequestClose?.Invoke();
        }

        // Registrace uživatele
        public void Register()
        {
            try
            {
                // základní validace
                if (string.IsNullOrWhiteSpace(FirstName) ||
                string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrEmpty(Password))
            {
                var err = "Vyplňte jméno, příjmení, e-mail a heslo.";
                RegistrationFailed?.Invoke(err);
                RequestMessage?.Invoke(err);
                return;
            }

            // pokud jde o studenta, ověřit rok narození
            if (IsStudent)
            {
                if (string.IsNullOrWhiteSpace(BirthYear) || !int.TryParse(BirthYear, out var by))
                {
                    var err = "U studenta zadejte platný rok narození.";
                    RegistrationFailed?.Invoke(err);
                    RequestMessage?.Invoke(err);
                    return;
                }

                var thisYear = DateTime.Now.Year;
                if (by < 1900 || by > thisYear)
                {
                    var err = "Rok narození musí být mezi 1900 a " + thisYear;
                    RegistrationFailed?.Invoke(err);
                    RequestMessage?.Invoke(err);
                    return;
                }
            }

            if (Password != ConfirmPassword)
            {
                var err = "Hesla se neshodují.";
                RegistrationFailed?.Invoke(err);
                RequestMessage?.Invoke(err);
                return;
            }

            try
            {
                using var ctx = new AppDbContext();

                    // zajistit defaultní adresu pokud není žádná
                    if (ctx.Adresa.Count() == 0)
                    {
                        var defaultAddr = new Adresa
                        {
                            // не указывайте IdAdresa, если БД генерирует его
                            Obec = "Nezadáno",
                            Ulice = "Nezadáno",
                            Psc = 0
                        };
                        ctx.Adresa.Add(defaultAddr);
                        ctx.SaveChanges();
                    }

                    // получить id адресы безопасно
                    var firstAddr = ctx.Adresa
                        .OrderBy(a => a.IdAdresa)
                        .FirstOrDefault();

                    if (firstAddr == null)
                    {
                        // на всякий случай — если всё ещё нет адресy, создаём и сохраняем
                        var fallback = new Adresa { Obec = "Nezadáno", Ulice = "Nezadáno", Psc = 0 };
                        ctx.Adresa.Add(fallback);
                        ctx.SaveChanges();
                        firstAddr = fallback;
                    }

                    var addrId = firstAddr.IdAdresa;

                    // kontrola existence uživatele se stejným emailem
                    if (ctx.Stravnik.Count(s => s.Email == Email) > 0)
                {
                    var err = "Uživatel s tímto e-mailem již existuje.";
                    RegistrationFailed?.Invoke(err);
                    RequestMessage?.Invoke(err);
                    return;
                }

                    // zjistit zda je to první uživatel (před vytvořením)
                    var isFirst = ctx.Stravnik.Count() == 0;

                    // spočítat IdStravnik (v produkci použijte DB sekvenci)
                    // var nextId = ctx.Stravnik.Any() ? ctx.Stravnik.Max(s => s.IdStravnik) + 1 : 1;

                    var hashed = BCrypt.Net.BCrypt.HashPassword(Password);

                var typ = IsWorker ? "pr" : "st";
                var role = isFirst ? "ADMIN" : "USER";

                // pokud je to první uživatel, zajistit existenci pozice systémového administrátora
                int? adminPoziceId = null;
                if (isFirst)
                {
                    var adminPoz = ctx.Pozice.FirstOrDefault(p => p.Nazev == "Systémový administrátor");
                    if (adminPoz == null)
                    {
                        adminPoz = new Pozice { Nazev = "Systémový administrátor" };
                        ctx.Pozice.Add(adminPoz);
                        ctx.SaveChanges();
                    }
                    adminPoziceId = adminPoz.IdPozice;

                    // pokud první uživatel je pracovník, zajistit že bude mít tuto pozici
                    if (IsWorker)
                    {
                        PositionId = adminPoziceId;
                    }
                }

                var stravnik = new Stravnik
                {
                    Jmeno = FirstName,
                    Prijmeni = LastName,
                    Email = Email,
                    Heslo = hashed,
                    Zustatek = 0,
                    Role = role,
                    Aktivita = '1',
                    TypStravnik = typ,
                    IdAdresa = addrId
                };

                ctx.Stravnik.Add(stravnik);
                ctx.SaveChanges();

                // uložit typ-specifické záznamy
                if (IsWorker)
                {
                    var telefon = int.TryParse(Phone, out var tel) ? tel : 0;
                    var poziceIdToUse = PositionId ?? adminPoziceId ?? 1;

                    var prac = new Pracovnik
                    {
                        IdStravnik = stravnik.IdStravnik,
                        Telefon = telefon,
                        IdPozice = poziceIdToUse
                    };
                    ctx.Pracovnik.Add(prac);
                }
                else
                {
                    // nastavit datum narození z roku (1.1.<rok>)
                    DateTime datum;
                    if (int.TryParse(BirthYear, out var by))
                        datum = new DateTime(by, 1, 1);
                    else
                        datum = DateTime.Now;

                    var stud = new Student
                    {
                        IdStravnik = stravnik.IdStravnik,
                        IdTrida = ClassId ?? 1,
                        DatumNarozeni = datum
                    };
                    ctx.Student.Add(stud);
                }

                // pokud chcete ukládat fotku do DB, doplňte zde logiku (např. create Soubor/byte[])
                ctx.SaveChanges();

                var msg = isFirst
                    ? "Registrace úspěšná. První uživatel byl vytvořen jako administrátor."
                    : "Registrace byla úspěšná.";

                RegistrationSucceeded?.Invoke(stravnik.Email, IsWorker, isFirst);
                RequestMessage?.Invoke(msg);
                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                var err = "Chyba při registraci: " + ex.Message;
                RegistrationFailed?.Invoke(err);
                RequestMessage?.Invoke(err);
            }
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "save-error.log"), ex.ToString() + Environment.NewLine);
                var err = "Chyba při registraci: " + ex.Message;
                RegistrationFailed?.Invoke(err);
                RequestMessage?.Invoke(err);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}