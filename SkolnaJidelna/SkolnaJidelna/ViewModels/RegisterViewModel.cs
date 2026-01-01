using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using SkolniJidelna.Services;
using System;
using System.IO;
using System.Linq;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;

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
        private string _birthYear = "2000"; // default selected year aligns with ComboBox default
        private string _ulice = string.Empty; // input that contains PSC, street+number, city
        private ObservableCollection<string> _years = new ObservableCollection<string>();

        private readonly IFileDialogService _fileDialogService;

        // Commands
        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand SelectPhotoCommand { get; }

        // Positions for UI
        public ObservableCollection<Pozice> Positions { get; private set; } = new ObservableCollection<Pozice>();

        // Classes for UI
        public ObservableCollection<Trida> Classes { get; private set; } = new ObservableCollection<Trida>();

        // Years for birth year ComboBox
        public ObservableCollection<string> Years
        {
            get
            {
                if (_years.Count == 0)
                {
                    LoadYears();
                }
                return _years;
            }
            private set
            {
                if (!ReferenceEquals(_years, value))
                {
                    _years = value;
                    OnPropertyChanged(nameof(Years));
                }
            }
        }

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
            SelectedBirthDate = new DateTime(int.Parse(BirthYear), 1, 1);
            LoadClasses();
            LoadYears();
        }

        /// <summary>
        /// Načte pozice z databáze a přidá výchozí, pokud neexistují.
        /// </summary>
        private void LoadPositions()
        {
            using var ctx = new AppDbContext();
            if (ctx.Pozice == null)
                return;

            if (ctx.Pozice.Count() == 0)
            {
                var adminPoz = new Pozice { Nazev = "Systémový administrátor" };
                ctx.Pozice.Add(adminPoz);
                var kucharPoz = new Pozice { Nazev = "Kuchař" };
                ctx.Pozice.Add(kucharPoz);
                var uklizechkaPoz = new Pozice { Nazev = "Uklízečka" };
                ctx.Pozice.Add(uklizechkaPoz);
                ctx.SaveChanges();
            }

            var list = ctx.Pozice.OrderBy(p => p.IdPozice).ToList();
            Positions = new ObservableCollection<Pozice>(list);
            if (Positions.Count == 0)
            {
                Positions.Add(new Pozice { IdPozice = 1, Nazev = "Test Admin" });
                Positions.Add(new Pozice { IdPozice = 2, Nazev = "Test Cook" });
            }
            OnPropertyChanged(nameof(Positions));
        }

        /// <summary>
        /// Načte třídy z databáze a přidá výchozí, pokud neexistují.
        /// </summary>
        private void LoadClasses()
        {
            using var ctx = new AppDbContext();
            if (ctx.Trida == null)
                return;

            if (ctx.Trida.Count() == 0)
            {
                for (int i = 1; i <= 9; i++)
                {
                    ctx.Trida.Add(new Trida { CisloTridy = i });
                }
                ctx.SaveChanges();
            }

            var list = ctx.Trida.OrderBy(t => t.CisloTridy).ToList();
            Classes = new ObservableCollection<Trida>(list);
            OnPropertyChanged(nameof(Classes));
        }

        /// <summary>
        /// Naplní seznam roků (1900..aktuální) pro výběr roku narození.
        /// </summary>
        private void LoadYears()
        {
            _years.Clear();
            var end = DateTime.Now.Year;
            for (int y = end; y >= 1900; y--)
            {
                _years.Add(y.ToString());
            }
            // set default if needed
            if (!string.IsNullOrWhiteSpace(BirthYear) && !_years.Contains(BirthYear))
            {
                BirthYear = end.ToString();
            }
            OnPropertyChanged(nameof(Years));
        }

        // Vlastnosti pro binding
        public string FirstName { get => _firstName; set { if (_firstName == value) return; _firstName = value; OnPropertyChanged(nameof(FirstName)); } }
        public string LastName { get => _lastName; set { if (_lastName == value) return; _lastName = value; OnPropertyChanged(nameof(LastName)); } }
        public string Email { get => _email; set { if (_email == value) return; _email = value; OnPropertyChanged(nameof(Email)); } }
        public string Password { get => _password; set { if (_password == value) return; _password = value; OnPropertyChanged(nameof(Password)); } }
        public string ConfirmPassword { get => _confirmPassword; set { if (_confirmPassword == value) return; _confirmPassword = value; OnPropertyChanged(nameof(ConfirmPassword)); } }

        // Address input: expected format (recommended): "<PSC>, <Ulice a číslo>, <Město (může mít více slov)>"
        // Binding is set in LoginWindow.xaml -> Text="{Binding Ulice, ...}"
        public string Ulice { get => _ulice; set { if (_ulice == value) return; _ulice = value; OnPropertyChanged(nameof(Ulice)); } }

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

        // Selected birth date for Calendar binding
        private DateTime? _selectedBirthDate;
        public DateTime? SelectedBirthDate
        {
            get => _selectedBirthDate;
            set
            {
                if (_selectedBirthDate == value) return;
                _selectedBirthDate = value;
                if (value.HasValue)
                {
                    BirthYear = value.Value.Year.ToString();
                }
                else
                {
                    BirthYear = string.Empty;
                }
                OnPropertyChanged(nameof(SelectedBirthDate));
            }
        }


        /// <summary>
        /// Otevře dialog pro výběr fotografie a nastaví cestu k souboru.
        /// </summary>
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

        /// <summary>
        /// Zruší registraci a zavře okno.
        /// </summary>
        public void Cancel()
        {
            CancelRequested?.Invoke();
            RequestClose?.Invoke();
        }

        /// <summary>
        /// Registruje nového uživatele (pracovního nebo studenta) do databáze.
        /// </summary>
        public void Register()
        {
            try
            {
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

                if (IsStudent)
                {
                    if (!SelectedBirthDate.HasValue)
                    {
                        var err = "U studenta zadejte platné datum narození.";
                        RegistrationFailed?.Invoke(err);
                        RequestMessage?.Invoke(err);
                        return;
                    }

                    var by = SelectedBirthDate.Value.Year;
                    var thisYear = DateTime.Now.Year;
                    if (by < 1900 || by > thisYear || SelectedBirthDate.Value > DateTime.Today)
                    {
                        var err = "Datum narození musí být mezi 1.1.1900 a dnešním dnem.";
                        RegistrationFailed!.Invoke(err);
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

                using var ctx = new AppDbContext();

                var isFirst = ctx.Stravnik.Count() == 0;
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
                    if (IsWorker) PositionId = adminPoziceId;
                }

                // If any active user with same email exists -> cannot register
                if (ctx.Stravnik.Count(s => s.Email == Email && s.Aktivita == '1') > 0)
                {
                    var err = "Uživatel s tímto e-mailem již existuje.";
                    RegistrationFailed?.Invoke(err);
                    RequestMessage?.Invoke(err);
                    return;
                }

                var typ = IsWorker ? "pr" : "st";

                // Require an existing inactive strávník matching name, surname, email and typ (per new proc contract)
                var candidate = ctx.Stravnik
                    .AsNoTracking()
                    .FirstOrDefault(s => s.Email == Email
                                         && s.Jmeno == FirstName
                                         && s.Prijmeni == LastName
                                         && (s.TypStravnik == typ || s.TypStravnik == null)
                                         && s.Aktivita == '0');
                if (candidate == null)
                {
                    var err = "Neexistuje předregistrovaný neaktivní uživatel se zadanými údaji (jméno, příjmení, e-mail, typ). Registrace není možná.";
                    RegistrationFailed?.Invoke(err);
                    RequestMessage?.Invoke(err);
                    return;
                }

                // Validate required selections to avoid passing NULLs into NOT NULL DB columns
                if (IsWorker && (PositionId == null))
                {
                    var err = "Vyberte prosím pozici pracovníka.";
                    RegistrationFailed?.Invoke(err);
                    RequestMessage?.Invoke(err);
                    return;
                }

                if (!IsWorker && (ClassId == null))
                {
                    var err = "Vyberte prosím třídu pro studenta.";
                    RegistrationFailed?.Invoke(err);
                    RequestMessage?.Invoke(err);
                    return;
                }

                int psc = 0;
                string mesto = "Nezadáno";
                string ulice = "Nezadáno";

                if (!string.IsNullOrWhiteSpace(Ulice))
                {
                    var parts = Ulice.Trim()
                                      .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(p => p.Trim()).ToArray();
                    if (parts.Length >= 3 && int.TryParse(parts[0], out var parsedPsc))
                    {
                        psc = parsedPsc;
                        ulice = parts[1];
                        mesto = string.Join(", ", parts.Skip(2));
                    }
                    else
                    {
                        var sp = Regex.Split(Ulice.Trim(), @"\s+");
                        if (sp.Length >= 3 && int.TryParse(sp[0], out var parsedPsc2))
                        {
                            psc = parsedPsc2;
                            mesto = sp[^1];
                            ulice = string.Join(" ", sp.Skip(1).Take(sp.Length - 2));
                        }
                        else
                        {
                            RequestMessage?.Invoke("Adresa není ve formátu '00000, Ulice 10, Město'. Použity výchozí hodnoty adresy.");
                        }
                    }
                }

                var hashed = BCrypt.Net.BCrypt.HashPassword(Password);
                var role = isFirst ? "ADMIN" : "USER";

                // Prepare photo bytes if present
                byte[]? photoBytes = null;
                string? photoName = null;
                string? photoExt = null;
                string? photoMime = null;
                if (!string.IsNullOrWhiteSpace(PhotoPath) && File.Exists(PhotoPath))
                {
                    try
                    {
                        photoBytes = File.ReadAllBytes(PhotoPath);
                        photoName = Path.GetFileNameWithoutExtension(PhotoPath);
                        photoExt = Path.GetExtension(PhotoPath).TrimStart('.').ToLowerInvariant();
                        photoMime = photoExt switch
                        {
                            "jpg" or "jpeg" => "image/jpeg",
                            "png" => "image/png",
                            "gif" => "image/gif",
                            _ => "application/octet-stream",
                        };
                    }
                    catch
                    {
                        // ignore photo if failed to read
                        photoBytes = null;
                        photoName = null;
                        photoExt = null;
                        photoMime = null;
                    }
                }

                // Only treat as photo when bytes exist and length > 0
                var hasPhoto = photoBytes != null && photoBytes.Length > 0;

                // Připravíme připojení a spustíme proceduru trans_register_pracovnik / trans_register_student
                var conn = ctx.Database.GetDbConnection();
                try
                {
                    if (conn.State != ConnectionState.Open) conn.Open();

                    if (IsWorker)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandType = CommandType.StoredProcedure;
                        ((Oracle.ManagedDataAccess.Client.OracleCommand)cmd).BindByName = true;
                        cmd.CommandText = "trans_register_pracovnik";

                        cmd.Parameters.Add(new OracleParameter("p_psc", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = psc });
                        cmd.Parameters.Add(new OracleParameter("p_mesto", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = (object)mesto ?? DBNull.Value });
                        cmd.Parameters.Add(new OracleParameter("p_ulice", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = (object)ulice ?? DBNull.Value });
                        cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = Email });
                        cmd.Parameters.Add(new OracleParameter("p_heslo", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = hashed });
                        // p_zustatek očekává FLOAT -> používáme Double
                        cmd.Parameters.Add(new OracleParameter("p_zustatek", OracleDbType.Double) { Direction = ParameterDirection.Input, Value = 0d });

                        var phoneDigits = Regex.Replace(Phone ?? string.Empty, @"\D", "");
                        int? telVal = null;
                        if (!string.IsNullOrWhiteSpace(phoneDigits) && int.TryParse(phoneDigits, out var parsedTel))
                        {
                            telVal = parsedTel;
                        }
                        cmd.Parameters.Add(new OracleParameter("p_telefon", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = (object?)telVal ?? DBNull.Value });

                        var pozToUse = PositionId ?? adminPoziceId ?? 1;
                        cmd.Parameters.Add(new OracleParameter("p_pozice", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = pozToUse });

                        // photo params
                        cmd.Parameters.Add(new OracleParameter("p_foto", OracleDbType.Blob) { Direction = ParameterDirection.Input, Value = hasPhoto ? (object)photoBytes! : DBNull.Value });
                        cmd.Parameters.Add(new OracleParameter("p_foto_nazev", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = hasPhoto ? (object)photoName! : DBNull.Value });
                        cmd.Parameters.Add(new OracleParameter("p_foto_pripona", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = hasPhoto ? (object)photoExt! : DBNull.Value });
                        cmd.Parameters.Add(new OracleParameter("p_foto_typ", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = hasPhoto ? (object)photoMime! : DBNull.Value });

                        // Console logging before execution
                        try
                        {
                            Console.WriteLine("Calling stored procedure: trans_register_pracovnik");
                            foreach (OracleParameter p in ((Oracle.ManagedDataAccess.Client.OracleCommand)cmd).Parameters)
                            {
                                var valStr = p.Value == null ? "<null>" : p.Value == DBNull.Value ? "<DBNULL>" : p.Value.ToString();
                                Console.WriteLine($"  {p.ParameterName} ({p.OracleDbType}) = {valStr}");
                            }
                        }
                        catch { /* ignore console issues */ }

                        try
                        {
                            cmd.ExecuteNonQuery();

                            RegistrationSucceeded?.Invoke(Email, true, isFirst);
                            RequestMessage?.Invoke("Registrace pracovníka byla úspěšná.");
                            RequestClose?.Invoke();
                        }
                        catch (OracleException oex)
                        {
                            // Detailed logging of parameters for debugging ORA errors
                            try
                            {
                                var sb = new System.Text.StringBuilder();
                                sb.AppendLine($"OracleException Number={oex.Number}, Message={oex.Message}");
                                sb.AppendLine("Parameters:");
                                foreach (OracleParameter p in ((Oracle.ManagedDataAccess.Client.OracleCommand)cmd).Parameters)
                                {
                                    sb.AppendLine($"  {p.ParameterName} ({p.OracleDbType}) = {(p.Value == null ? "<null>" : p.Value == DBNull.Value ? "<DBNULL>" : p.Value.ToString())}");
                                }
                                sb.AppendLine(oex.StackTrace);
                                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "save-error.log"), sb.ToString());

                                // Console log too
                                Console.WriteLine(sb.ToString());
                            }
                            catch { }

                            var err = oex.Message.Contains("Strávník neexistuje") ? "Strávník neexistuje nebo je již aktivní." : ("Chyba DB při registraci pracovníka: " + oex.Message + " (ORA-" + oex.Number + ")");
                            RegistrationFailed?.Invoke(err);
                            RequestMessage?.Invoke(err);
                        }
                    }
                    else
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandType = CommandType.StoredProcedure;
                        ((Oracle.ManagedDataAccess.Client.OracleCommand)cmd).BindByName = true;
                        cmd.CommandText = "trans_register_student";

                        cmd.Parameters.Add(new OracleParameter("p_psc", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = psc });
                        cmd.Parameters.Add(new OracleParameter("p_mesto", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = (object)mesto ?? DBNull.Value });
                        cmd.Parameters.Add(new OracleParameter("p_ulice", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = (object)ulice ?? DBNull.Value });
                        cmd.Parameters.Add(new OracleParameter("p_email", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = Email });
                        cmd.Parameters.Add(new OracleParameter("p_heslo", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = hashed });
                        // p_zustatek očekává FLOAT -> používáme Double
                        cmd.Parameters.Add(new OracleParameter("p_zustatek", OracleDbType.Double) { Direction = ParameterDirection.Input, Value = 0d });

                        if (SelectedBirthDate.HasValue)
                        {
                            cmd.Parameters.Add(new OracleParameter("p_rok_narozeni", OracleDbType.Date) { Direction = ParameterDirection.Input, Value = SelectedBirthDate.Value });
                        }
                        else
                        {
                            cmd.Parameters.Add(new OracleParameter("p_rok_narozeni", OracleDbType.Date) { Direction = ParameterDirection.Input, Value = DBNull.Value });
                        }

                        // передаём identifikátor záznamu TRIDY (ID_TRIDA), jak očekává procedura
                        cmd.Parameters.Add(new OracleParameter("p_cislo_tridy", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = (object?)ClassId ?? DBNull.Value });

                        // photo params
                        cmd.Parameters.Add(new OracleParameter("p_foto", OracleDbType.Blob) { Direction = ParameterDirection.Input, Value = hasPhoto ? (object)photoBytes! : DBNull.Value });
                        cmd.Parameters.Add(new OracleParameter("p_foto_nazev", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = hasPhoto ? (object)photoName! : DBNull.Value });
                        cmd.Parameters.Add(new OracleParameter("p_foto_pripona", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = hasPhoto ? (object)photoExt! : DBNull.Value });
                        cmd.Parameters.Add(new OracleParameter("p_foto_typ", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = hasPhoto ? (object)photoMime! : DBNull.Value });

                        // Console logging before execution
                        try
                        {
                            Console.WriteLine("Calling stored procedure: trans_register_student");
                            foreach (OracleParameter p in ((Oracle.ManagedDataAccess.Client.OracleCommand)cmd).Parameters)
                            {
                                var valStr = p.Value == null ? "<null>" : p.Value == DBNull.Value ? "<DBNULL>" : p.Value.ToString();
                                Console.WriteLine($"  {p.ParameterName} ({p.OracleDbType}) = {valStr}");
                            }
                        }
                        catch { /* ignore console issues */ }

                        try
                        {
                            cmd.ExecuteNonQuery();

                            RegistrationSucceeded?.Invoke(Email, false, isFirst);
                            RequestMessage?.Invoke("Registrace studenta byla úspěšná.");
                            RequestClose?.Invoke();
                        }
                        catch (OracleException oex)
                        {
                            try
                            {
                                var sb = new System.Text.StringBuilder();
                                sb.AppendLine($"OracleException Number={oex.Number}, Message={oex.Message}");
                                sb.AppendLine("Parameters:");
                                foreach (OracleParameter p in ((Oracle.ManagedDataAccess.Client.OracleCommand)cmd).Parameters)
                                {
                                    sb.AppendLine($"  {p.ParameterName} ({p.OracleDbType}) = {(p.Value == null ? "<null>" : p.Value == DBNull.Value ? "<DBNULL>" : p.Value.ToString())}");
                                }
                                sb.AppendLine(oex.StackTrace);
                                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "save-error.log"), sb.ToString());

                                // Console log too
                                Console.WriteLine(sb.ToString());
                            }
                            catch { }

                            var err = oex.Message.Contains("Strávník neexistuje") ? "Strávník neexistuje nebo je již aktivní." : ("Chyba DB při registraci studenta: " + oex.Message + " (ORA-" + oex.Number + ")");
                            RegistrationFailed?.Invoke(err);
                            RequestMessage?.Invoke(err);
                        }
                    }
                }
                finally
                {
                    try { if (conn.State == ConnectionState.Open) conn.Close(); } catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "save-error.log"), ex.ToString() + Environment.NewLine);
                try { Console.WriteLine("Unhandled error in Register(): " + ex); } catch { }
                var err = "Chyba při registraci: " + ex.Message;
                RegistrationFailed?.Invoke(err);
                RequestMessage?.Invoke(err);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}