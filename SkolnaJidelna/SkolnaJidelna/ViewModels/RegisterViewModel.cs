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
        private string _birthYear = string.Empty;
        private string _ulice = string.Empty; // input that contains PSC, street+number, city

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
                var adminPoz = new Pozice { IdPozice = 1, Nazev = "Systémový administrátor" };
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

        // Registrace uživatele — nyní voláme PL/SQL trans_register_* procedury
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

                using var ctx = new AppDbContext();

                // zajistit existence systémové pozice pokud je to první uživatel
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

                // kontrola lokální duplicity (rychlá zpětná vazba)
                if (ctx.Stravnik.Count(s => s.Email == Email) > 0)
                {
                    var err = "Uživatel s tímto e-mailem již existuje.";
                    RegistrationFailed?.Invoke(err);
                    RequestMessage?.Invoke(err);
                    return;
                }

                // parsování adresy -> předáme p_psc, p_mesto, p_ulice proceduře
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
                        // nepovinné: podpora jednoduchého prostoru-separated formátu "00000 Ulice 10 Mesto"
                        var sp = Regex.Split(Ulice.Trim(), @"\s+");
                        if (sp.Length >= 3 && int.TryParse(sp[0], out var parsedPsc2))
                        {
                            psc = parsedPsc2;
                            mesto = sp[^1];
                            ulice = string.Join(" ", sp.Skip(1).Take(sp.Length - 2));
                        }
                        else
                        {
                            RequestMessage?.Invoke("Adresa není ve formátu '00000, Ulice 10, Mesto'. Použity výchozí hodnoty adresy.");
                        }
                    }
                }

                var hashed = BCrypt.Net.BCrypt.HashPassword(Password);
                var typ = IsWorker ? "pr" : "st";
                var role = isFirst ? "ADMIN" : "USER";

                // Připravíme připojení a spustíme proceduru trans_register_pracovnik / trans_register_student
                var conn = ctx.Database.GetDbConnection();
                try
                {
                    if (conn.State != ConnectionState.Open) conn.Open();

                    if (IsWorker)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandType = CommandType.Text;
                        ((Oracle.ManagedDataAccess.Client.OracleCommand)cmd).BindByName = true;
                        // call the transaction proc in an anonymous block to avoid possible parameter binding quirks
                        cmd.CommandText = "BEGIN trans_register_pracovnik(:p_psc, :p_mesto, :p_ulice, :p_jmeno, :p_prijmeni, :p_email, :p_heslo, :p_zustatek, :p_telefon, :p_pozice); END;";

                        // p_psc, p_mesto, p_ulice
                        var p1 = new OracleParameter("p_psc", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = psc };
                        var p2 = new OracleParameter("p_mesto", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = (object)mesto ?? DBNull.Value };
                        var p3 = new OracleParameter("p_ulice", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = (object)ulice ?? DBNull.Value };

                        // osobní údaje
                        var p4 = new OracleParameter("p_jmeno", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = FirstName };
                        var p5 = new OracleParameter("p_prijmeni", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = LastName };
                        var p6 = new OracleParameter("p_email", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = Email };
                        var p7 = new OracleParameter("p_heslo", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = hashed };

                        var p8 = new OracleParameter("p_zustatek", OracleDbType.Decimal) { Direction = ParameterDirection.Input, Value = 0 };

                        // telefon a pozice
                        // vezmeme hodnotu z textBoxPhone (vlastnost Phone),
                        // odstraníme nečíselné znaky a pokud parsování selže, použijeme 0.
                        var phoneDigits = Regex.Replace(Phone ?? string.Empty, @"\D", "");
                        int telVal = 0;
                        if (!string.IsNullOrWhiteSpace(phoneDigits) && int.TryParse(phoneDigits, out var parsedTel))
                        {
                            telVal = parsedTel;
                        }

                        var p9 = new OracleParameter("p_telefon", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = telVal };

                        var pozToUse = PositionId ?? adminPoziceId ?? 1;
                        var p10 = new OracleParameter("p_pozice", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = pozToUse };

                        cmd.Parameters.Add(p1);
                        cmd.Parameters.Add(p2);
                        cmd.Parameters.Add(p3);
                        cmd.Parameters.Add(p4);
                        cmd.Parameters.Add(p5);
                        cmd.Parameters.Add(p6);
                        cmd.Parameters.Add(p7);
                        cmd.Parameters.Add(p8);
                        cmd.Parameters.Add(p9);
                        cmd.Parameters.Add(p10);

                        try
                        {
                            cmd.ExecuteNonQuery();

                            // Pokusíme se uložit fotku do tabulky SOUBORY (pokud byla vybrána)
                            if (!string.IsNullOrWhiteSpace(PhotoPath) && File.Exists(PhotoPath))
                            {
                                try
                                {
                                    // zjistit id_stravnik pro právě vytvořeného uživatele
                                    using var idCmd = conn.CreateCommand();
                                    ((Oracle.ManagedDataAccess.Client.OracleCommand)idCmd).BindByName = true;
                                    idCmd.CommandType = CommandType.Text;
                                    idCmd.CommandText = "SELECT id_stravnik FROM stravnici WHERE email = :email";
                                    idCmd.Parameters.Add(new OracleParameter("email", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = Email });
                                    var idObj = idCmd.ExecuteScalar();
                                    if (idObj != null && int.TryParse(idObj.ToString(), out var idStravnik))
                                    {
                                        var fileBytes = File.ReadAllBytes(PhotoPath);
                                        var fileName = Path.GetFileName(PhotoPath);
                                        var ext = Path.GetExtension(PhotoPath).TrimStart('.').ToLowerInvariant();
                                        var nameOnly = Path.GetFileNameWithoutExtension(PhotoPath);
                                        string mime = ext switch
                                        {
                                            "jpg" or "jpeg" => "image/jpeg",
                                            "png" => "image/png",
                                            "gif" => "image/gif",
                                            _ => "application/octet-stream",
                                        };

                                        using var insCmd = conn.CreateCommand();
                                        ((Oracle.ManagedDataAccess.Client.OracleCommand)insCmd).BindByName = true;
                                        insCmd.CommandType = CommandType.Text;
                                        insCmd.CommandText = "INSERT INTO soubory (id_soubor, nazev, typ, pripona, obsah, datum_nahrani, tabulka, id_zaznam, id_stravnik) VALUES (s_soub.NEXTVAL, :nazev, :typ, :pripona, :obsah, :datum_nahrani, :tabulka, :id_zaznam, :id_stravnik)";

                                        insCmd.Parameters.Add(new OracleParameter("nazev", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = nameOnly });
                                        insCmd.Parameters.Add(new OracleParameter("typ", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = mime });
                                        insCmd.Parameters.Add(new OracleParameter("pripona", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = ext });
                                        insCmd.Parameters.Add(new OracleParameter("obsah", OracleDbType.Blob) { Direction = ParameterDirection.Input, Value = fileBytes });
                                        insCmd.Parameters.Add(new OracleParameter("datum_nahrani", OracleDbType.Date) { Direction = ParameterDirection.Input, Value = DateTime.Now });
                                        insCmd.Parameters.Add(new OracleParameter("tabulka", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = "STRAVNICI" });
                                        insCmd.Parameters.Add(new OracleParameter("id_zaznam", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = idStravnik });
                                        insCmd.Parameters.Add(new OracleParameter("id_stravnik", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = idStravnik });

                                        insCmd.ExecuteNonQuery();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Nechceme failovat samotnou registraci kvůli problému s ukládáním fotky
                                    RequestMessage?.Invoke("Uložení fotky se nezdařilo: " + ex.Message);
                                }
                            }

                            RegistrationSucceeded?.Invoke(Email, true, isFirst);
                            RequestMessage?.Invoke("Registrace pracovníka byla úspěšná.");
                            RequestClose?.Invoke();
                        }
                        catch (OracleException oex)
                        {
                            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "save-error.log"), $"OracleException Number={oex.Number}, Message={oex.Message}, Stack={oex.StackTrace}{Environment.NewLine}");
                            if (oex.Number == 20001 || (oex.Message?.Contains("Uživatel s tímto e-mailem") ?? false))
                            {
                                var err = "Uživatel s tímto e-mailem již existuje.";
                                RegistrationFailed?.Invoke(err);
                                RequestMessage?.Invoke(err);
                            }
                            else
                            {
                                var err = "Chyba DB při registraci pracovníka: " + oex.Message + " (ORA-" + oex.Number + ")";
                                RegistrationFailed?.Invoke(err);
                                RequestMessage?.Invoke(err);
                            }
                        }
                    }
                    else // student
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandType = CommandType.Text;
                        ((Oracle.ManagedDataAccess.Client.OracleCommand)cmd).BindByName = true;
                        cmd.CommandText = "BEGIN trans_register_student(:p_psc, :p_mesto, :p_ulice, :p_jmeno, :p_prijmeni, :p_email, :p_heslo, :p_zustatek, :p_rok_narozeni, :p_cislo_tridy); END;";

                        // address
                        var p1 = new OracleParameter("p_psc", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = psc };
                        var p2 = new OracleParameter("p_mesto", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = (object)mesto ?? DBNull.Value };
                        var p3 = new OracleParameter("p_ulice", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = (object)ulice ?? DBNull.Value };

                        // osobní údaje
                        var p4 = new OracleParameter("p_jmeno", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = FirstName };
                        var p5 = new OracleParameter("p_prijmeni", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = LastName };
                        var p6 = new OracleParameter("p_email", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = Email };
                        var p7 = new OracleParameter("p_heslo", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = hashed };
                        var p8 = new OracleParameter("p_zustatek", OracleDbType.Decimal) { Direction = ParameterDirection.Input, Value = 0 };

                        // datum narozeni jako DATE
                        OracleParameter p9;
                        if (int.TryParse(BirthYear, out var by))
                        {
                            // store as DATE with year only -> use Jan 1 of that year, time 00:00:00
                            var dt = new DateTime(by, 1, 1);
                            p9 = new OracleParameter("p_rok_narozeni", OracleDbType.Date) { Direction = ParameterDirection.Input, Value = new OracleDate(dt) };
                        }
                        else
                        {
                            p9 = new OracleParameter("p_rok_narozeni", OracleDbType.Date) { Direction = ParameterDirection.Input, Value = DBNull.Value };
                        }

                        var p10 = new OracleParameter("p_cislo_tridy", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = (object?)ClassId ?? DBNull.Value };

                        cmd.Parameters.Add(p1);
                        cmd.Parameters.Add(p2);
                        cmd.Parameters.Add(p3);
                        cmd.Parameters.Add(p4);
                        cmd.Parameters.Add(p5);
                        cmd.Parameters.Add(p6);
                        cmd.Parameters.Add(p7);
                        cmd.Parameters.Add(p8);
                        cmd.Parameters.Add(p9);
                        cmd.Parameters.Add(p10);

                        try
                        {
                            cmd.ExecuteNonQuery();

                            // Pokusíme se uložit fotku do tabulky SOUBORY (pokud byla vybrána)
                            if (!string.IsNullOrWhiteSpace(PhotoPath) && File.Exists(PhotoPath))
                            {
                                try
                                {
                                    // zjistit id_stravnik pro právě vytvořeného uživatele
                                    using var idCmd = conn.CreateCommand();
                                    ((Oracle.ManagedDataAccess.Client.OracleCommand)idCmd).BindByName = true;
                                    idCmd.CommandType = CommandType.Text;
                                    idCmd.CommandText = "SELECT id_stravnik FROM stravnici WHERE email = :email";
                                    idCmd.Parameters.Add(new OracleParameter("email", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = Email });
                                    var idObj = idCmd.ExecuteScalar();
                                    if (idObj != null && int.TryParse(idObj.ToString(), out var idStravnik))
                                    {
                                        var fileBytes = File.ReadAllBytes(PhotoPath);
                                        var fileName = Path.GetFileName(PhotoPath);
                                        var ext = Path.GetExtension(PhotoPath).TrimStart('.').ToLowerInvariant();
                                        var nameOnly = Path.GetFileNameWithoutExtension(PhotoPath);
                                        string mime = ext switch
                                        {
                                            "jpg" or "jpeg" => "image/jpeg",
                                            "png" => "image/png",
                                            "gif" => "image/gif",
                                            _ => "application/octet-stream",
                                        };

                                        using var insCmd = conn.CreateCommand();
                                        ((Oracle.ManagedDataAccess.Client.OracleCommand)insCmd).BindByName = true;
                                        insCmd.CommandType = CommandType.Text;
                                        insCmd.CommandText = "INSERT INTO soubory (id_soubor, nazev, typ, pripona, obsah, datum_nahrani, tabulka, id_zaznam, id_stravnik) VALUES (s_soub.NEXTVAL, :nazev, :typ, :pripona, :obsah, :datum_nahrani, :tabulka, :id_zaznam, :id_stravnik)";

                                        insCmd.Parameters.Add(new OracleParameter("nazev", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = nameOnly });
                                        insCmd.Parameters.Add(new OracleParameter("typ", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = mime });
                                        insCmd.Parameters.Add(new OracleParameter("pripona", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = ext });
                                        insCmd.Parameters.Add(new OracleParameter("obsah", OracleDbType.Blob) { Direction = ParameterDirection.Input, Value = fileBytes });
                                        insCmd.Parameters.Add(new OracleParameter("datum_nahrani", OracleDbType.Date) { Direction = ParameterDirection.Input, Value = DateTime.Now });
                                        insCmd.Parameters.Add(new OracleParameter("tabulka", OracleDbType.Varchar2) { Direction = ParameterDirection.Input, Value = "STRAVNICI" });
                                        insCmd.Parameters.Add(new OracleParameter("id_zaznam", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = idStravnik });
                                        insCmd.Parameters.Add(new OracleParameter("id_stravnik", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = idStravnik });

                                        insCmd.ExecuteNonQuery();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    // Nechceme failovat samotnou registraci kvůli problému s ukládáním fotky
                                    RequestMessage?.Invoke("Uložení fotky se nezdařilo: " + ex.Message);
                                }
                            }

                            RegistrationSucceeded?.Invoke(Email, false, isFirst);
                            RequestMessage?.Invoke("Registrace studenta byla úspěšná.");
                            RequestClose?.Invoke();
                        }
                        catch (OracleException oex)
                        {
                            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "save-error.log"), $"OracleException Number={oex.Number}, Message={oex.Message}, Stack={oex.StackTrace}{Environment.NewLine}");
                            if (oex.Number == 20001 || (oex.Message?.Contains("Uživatel s tímto e-mailem") ?? false))
                            {
                                var err = "Uživatel s tímto e-mailem již existuje.";
                                RegistrationFailed?.Invoke(err);
                                RequestMessage?.Invoke(err);
                            }
                            else
                            {
                                var err = "Chyba DB při registraci studenta: " + oex.Message + " (ORA-" + oex.Number + ")";
                                RegistrationFailed?.Invoke(err);
                                RequestMessage?.Invoke(err);
                            }
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
                var err = "Chyba při registraci: " + ex.Message;
                RegistrationFailed?.Invoke(err);
                RequestMessage?.Invoke(err);
            }
        }

        private int EnsureDefaultAddressAndGetId(AppDbContext ctx)
        {
            if (ctx.Adresa.Count() == 0)
            {
                var defaultAddr = new Adresa { Mesto = "Nezadáno", Ulice = "Nezadáno", Psc = 0 };
                ctx.Adresa.Add(defaultAddr);
                ctx.SaveChanges();
            }

            var firstAddr = ctx.Adresa.OrderBy(a => a.IdAdresa).FirstOrDefault();
            if (firstAddr == null)
            {
                var fallback = new Adresa { Mesto = "Nezadáno", Ulice = "Nezadáno", Psc = 0 };
                ctx.Adresa.Add(fallback);
                ctx.SaveChanges();
                firstAddr = fallback;
            }

            return firstAddr.IdAdresa;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}