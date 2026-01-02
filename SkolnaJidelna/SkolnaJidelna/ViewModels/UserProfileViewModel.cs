using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Collections.ObjectModel;
using System.IO;

namespace SkolniJidelna.ViewModels
{
    // ViewModel uživatelského profilu – načítá a zobrazuje údaje pro daný e‑mail
    // Zobrazuje základní informace, adresu, status (student/pracovník), třídu/pozici,
    // alergie a dietní omezení (textově i jako výběr), a profilovou fotku
    public class UserProfileViewModel : INotifyPropertyChanged
    {
        // Stavové textové vlastnosti pro hlavičku profilu (jméno, zůstatek, email, telefon, adresa, status, třída/pozice, rok narození)
        private string _fullName = string.Empty;            // celé jméno (Jméno + Příjmení)
        private string _balanceFormatted = "0 Kč";         // zůstatek na účtu formátovaný pro UI
        private string _email = string.Empty;               // e‑mail uživatele – primární klíč pro načítání
        private string _phone = string.Empty;               // telefon (jen pro pracovníky)
        private string _address = string.Empty;             // adresa – skládá se z PSČ, ulice, města
        private string _status = string.Empty;              // status – student/pracovník
        private string _positionClass = string.Empty;       // třída (student) nebo pozice (pracovník)
        private string _rokNarozeni = string.Empty;         // rok narození (student)
        private ImageSource? _profileImage;                 // profilová fotka (soubor) nebo inicialy

        // Viditelnosti částí UI (textové vs. editační prvky) – řídí, co se v profilu zobrazí
        private Visibility _comboAlergiesVisibility = Visibility.Collapsed;        // zobrazení seznamu alergií (checkboxy)
        private Visibility _comboDietRestrictionsVisibility = Visibility.Collapsed; // zobrazení seznamu dietních omezení (checkboxy)
        private Visibility _saveButtonVisibility = Visibility.Collapsed;           // zobrazení tlačítka Uložit
        private Visibility _textAlergiesVisibility = Visibility.Visible;           // zobrazení textového souhrnu alergií
        private Visibility _textDietRestrictionsVisibility = Visibility.Visible;   // zobrazení textového souhrnu dietních omezení
        private string _alergiesText = string.Empty;                               // souhrnný text alergií (oddělený čárkou)
        private string _dietRestrictionsText = string.Empty;                       // souhrnný text dietních omezení (oddělený čárkou)
        private Visibility _rokVisibility = Visibility.Collapsed;                  // zobrazení roku narození (jen student)
        private Visibility _phoneVisibility = Visibility.Collapsed;                // zobrazení telefonu (jen pracovník)

        // Základní bindované vlastnosti – změna vyvolá OnPropertyChanged pro aktualizaci UI
        public string FullName { get => _fullName; private set { if (_fullName == value) return; _fullName = value; OnPropertyChanged(nameof(FullName)); } }
        public string BalanceFormatted { get => _balanceFormatted; private set { if (_balanceFormatted == value) return; _balanceFormatted = value; OnPropertyChanged(nameof(BalanceFormatted)); } }
        public string Email { get => _email; private set { if (_email == value) return; _email = value; OnPropertyChanged(nameof(Email)); } }
        public string Phone { get => _phone; private set { if (_phone == value) return; _phone = value; OnPropertyChanged(nameof(Phone)); } }
        public string Address { get => _address; private set { if (_address == value) return; _address = value; OnPropertyChanged(nameof(Address)); } }
        public string Status { get => _status; private set { if (_status == value) return; _status = value; OnPropertyChanged(nameof(Status)); } }
        public string PositionClass { get => _positionClass; private set { if (_positionClass == value) return; _positionClass = value; OnPropertyChanged(nameof(PositionClass)); } }
        public string RokNarozeni { get => _rokNarozeni; private set { if (_rokNarozeni == value) return; _rokNarozeni = value; OnPropertyChanged(nameof(RokNarozeni)); } }

        public ImageSource? ProfileImage { get => _profileImage; private set { if (_profileImage == value) return; _profileImage = value; OnPropertyChanged(nameof(ProfileImage)); } }

        // Viditelnosti a texty pro alergie/diety – dvojí režim zobrazení: souhrnný text vs. editační checkboxy
        public Visibility ComboAlergiesVisibility { get => _comboAlergiesVisibility; private set { if (_comboAlergiesVisibility == value) return; _comboAlergiesVisibility = value; OnPropertyChanged(nameof(ComboAlergiesVisibility)); } }
        public Visibility ComboDietRestrictionsVisibility { get => _comboDietRestrictionsVisibility; private set { if (_comboDietRestrictionsVisibility == value) return; _comboDietRestrictionsVisibility = value; OnPropertyChanged(nameof(ComboDietRestrictionsVisibility)); } }
        public Visibility SaveButtonVisibility { get => _saveButtonVisibility; private set { if (_saveButtonVisibility == value) return; _saveButtonVisibility = value; OnPropertyChanged(nameof(SaveButtonVisibility)); } }
        public Visibility TextAlergiesVisibility { get => _textAlergiesVisibility; private set { if (_textAlergiesVisibility == value) return; _textAlergiesVisibility = value; OnPropertyChanged(nameof(TextAlergiesVisibility)); } }
        public Visibility TextDietRestrictionsVisibility { get => _textDietRestrictionsVisibility; private set { if (_textDietRestrictionsVisibility == value) return; _textDietRestrictionsVisibility = value; OnPropertyChanged(nameof(TextDietRestrictionsVisibility)); } }
        public string AlergiesText { get => _alergiesText; private set { if (_alergiesText == value) return; _alergiesText = value; OnPropertyChanged(nameof(AlergiesText)); } }
        public string DietRestrictionsText { get => _dietRestrictionsText; private set { if (_dietRestrictionsText == value) return; _dietRestrictionsText = value; OnPropertyChanged(nameof(DietRestrictionsText)); } }
        public Visibility RokVisibility { get => _rokVisibility; private set { if (_rokVisibility == value) return; _rokVisibility = value; OnPropertyChanged(nameof(RokVisibility)); } }
        public Visibility PhoneVisibility { get => _phoneVisibility; private set { if (_phoneVisibility == value) return; _phoneVisibility = value; OnPropertyChanged(nameof(PhoneVisibility)); } }

        // Kolekce dostupných a vybraných alergií a diet – používá se pro editační režim (checkboxy)
        public ObservableCollection<Alergie> AvailableAlergies { get; } = new();
        public ObservableCollection<Alergie> SelectedAlergies { get; } = new();
        public ObservableCollection<DietniOmezeni> AvailableDietRestrictions { get; } = new();
        public ObservableCollection<DietniOmezeni> SelectedDietRestrictions { get; } = new();

        // Zjednodušené wrappery pro výběr (obsahují model + příznak IsSelected)
        public ObservableCollection<SelectableAlergie> SelectableAlergies { get; } = new();
        public ObservableCollection<SelectableDiet> SelectableDiets { get; } = new();

        // Událost pro View (např. skrytí spinneru po dokončení načítání)
        public event Action? LoadFinished;

        public UserProfileViewModel() { }

        public UserProfileViewModel(string email)
        {
            // Vstupní e‑mail je povinný – zahájí asynchronní načítání dat pro profil
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
            // fire-and-forget – načítání probíhá asynchronně, UI se aktualizuje přes PropertyChanged
            _ = LoadAsync(email.Trim());
        }

        // Hlavní metoda pro načtení všech údajů profilu: Stravnik, adresa, alergie/diety, student/pracovník specifika a fotka
        private async Task LoadAsync(string email)
        {
            try
            {
                using var db = new AppDbContext();

                // 1) Načtení záznamu Stravnik podle e‑mailu – základní identita uživatele
                var stravnik = await db.Stravnik
                    .AsNoTracking() // čtení bez sledování – výkon
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (stravnik == null)
                {
                    // Když uživatel neexistuje, nastavíme jen e‑mail a zobrazíme informaci
                    Email = email;
                    FullName = "Uživatel nenalezen";
                    return;
                }

                // 2) Základní vlastnosti
                Email = stravnik.Email;
                FullName = $"{stravnik.Jmeno} {stravnik.Prijmeni}";
                BalanceFormatted = string.Format("{0:0.##} Kč", stravnik.Zustatek);

                // map TypStravnik na lokalizovaný text (pr=pracovnik, st=student)
                var t = (stravnik.TypStravnik ?? string.Empty).Trim().ToLowerInvariant();
                if (t == "pr") Status = "pracovnik";
                else if (t == "st") Status = "student";
                else Status = stravnik.TypStravnik ?? string.Empty;

                // 3) Adresa – načítá se zvlášť kvůli oddělené tabulce
                try
                {
                    if (stravnik.IdAdresa != null)
                    {
                        var adresa = await db.Adresa.AsNoTracking().FirstOrDefaultAsync(a => a.IdAdresa == stravnik.IdAdresa);
                        if (adresa != null)
                            Address = $"{adresa.Psc} {adresa.Ulice}, {adresa.Mesto}".Trim();
                    }
                }
                catch (Exception ex)
                {
                    // Selhání načtení adresy nezastavuje zbytek profilu – jen vynuluje adresu
                    System.Diagnostics.Debug.WriteLine("Load Address failed: " + ex);
                    Address = string.Empty;
                }

                // 4) Alergie – načítá se přes vazební tabulku a hlavní tabulku ALERGIE
                try
                {
                    // Ověření existence tabulek pro aktuální DB uživatele – ochrana proti ORA-00942
                    bool hasAlergieTable = TableExists(db, "ALERGIE");
                    bool hasAlergieStravniciTable = TableExists(db, "STRAVNICI_ALERGIE");
                    if (!hasAlergieTable || !hasAlergieStravniciTable)
                    {
                        // Zalogovat chybějící tabulky a vynechat načítání
                        try { File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "missing-tables.log"), $"Skipping allergies load - ALERGIE present={hasAlergieTable}, STRAVNICI_ALERGIE present={hasAlergieStravniciTable}, Time={DateTime.Now:o}{Environment.NewLine}"); } catch { }
                        AlergiesText = string.Empty;
                    }
                    else
                    {
                        var allergyNames = await db.StravnikAlergie
                            .Where(sa => sa.IdStravnik == stravnik.IdStravnik)
                            .Join(db.Alergie, sa => sa.IdAlergie, a => a.IdAlergie, (sa, a) => a.Nazev)
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .ToListAsync();

                        // Souhrnný text (např. "Laktóza, Ořechy")
                        AlergiesText = allergyNames.Any() ? string.Join(", ", allergyNames) : string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Load Allergies failed: " + ex);
                    AlergiesText = string.Empty;
                }

                // 5) Dietní omezení – analogicky k alergiím
                try
                {
                    bool hasDietTable = TableExists(db, "DIETNI_OMEZENI");
                    bool hasOmezeniStravnikTable = TableExists(db, "STRAVNICI_OMEZENI");
                    if (!hasDietTable || !hasOmezeniStravnikTable)
                    {
                        try { File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "missing-tables.log"), $"Skipping diet restrictions load - DIETNI_OMEZENI present={hasDietTable}, STRAVNICI_OMEZENI present={hasOmezeniStravnikTable}, Time={DateTime.Now:o}{Environment.NewLine}"); } catch { }
                        DietRestrictionsText = string.Empty;
                    }
                    else
                    {
                        var dietNames = await db.StravnikOmezeni
                            .Where(so => so.IdStravnik == stravnik.IdStravnik)
                            .Join(db.DietniOmezeni, so => so.IdOmezeni, d => d.IdOmezeni, (so, d) => d.Nazev)
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .ToListAsync();

                        DietRestrictionsText = dietNames.Any() ? string.Join(", ", dietNames) : string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Load Diet restrictions failed: " + ex);
                    DietRestrictionsText = string.Empty;
                }

                // 6) Specifické údaje podle typu (student/pracovník) – nastaví viditelnosti a naplní editační seznamy
                try
                {
                    var typ = (stravnik.TypStravnik ?? string.Empty).Trim().ToLowerInvariant();
                    if (typ == "st" || string.Equals(stravnik.TypStravnik, "student", StringComparison.OrdinalIgnoreCase))
                    {
                        // Student – načti třídu, zobraz editační prvky (bez uložení), skryj texty, ukaž rok narození
                        var student = await db.Student
                            .Include(t2 => t2.Trida)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.IdStravnik == stravnik.IdStravnik);

                        PositionClass = student != null && student.Trida != null ? $"Třída: {student.Trida.CisloTridy}" : "-";

                        ComboAlergiesVisibility = Visibility.Visible;
                        ComboDietRestrictionsVisibility = Visibility.Visible;
                        SaveButtonVisibility = Visibility.Collapsed; // u studentů bez ukládacího tlačítka
                        TextAlergiesVisibility = Visibility.Collapsed;
                        TextDietRestrictionsVisibility = Visibility.Collapsed;

                        // Naplnění checkboxů – výčet všech a označení vybraných
                        SelectableAlergies.Clear();
                        var allAlergies = await db.Alergie.ToListAsync();
                        var selectedAlergieIds = await db.StravnikAlergie.Where(sa => sa.IdStravnik == stravnik.IdStravnik).Select(sa => sa.IdAlergie).ToListAsync();
                        foreach (var a in allAlergies)
                        {
                            SelectableAlergies.Add(new SelectableAlergie { Alergie = a, IsSelected = selectedAlergieIds.Contains(a.IdAlergie) });
                        }

                        SelectableDiets.Clear();
                        var allDiets = await db.DietniOmezeni.ToListAsync();
                        var selectedDietIds = await db.StravnikOmezeni.Where(so => so.IdStravnik == stravnik.IdStravnik).Select(so => so.IdOmezeni).ToListAsync();
                        foreach (var d in allDiets)
                        {
                            SelectableDiets.Add(new SelectableDiet { Diet = d, IsSelected = selectedDietIds.Contains(d.IdOmezeni) });
                        }

                        RokNarozeni = student != null ? student.DatumNarozeni.Year.ToString() : string.Empty;
                        RokVisibility = Visibility.Visible;
                        PhoneVisibility = Visibility.Collapsed;
                    }
                    else
                    {
                        // Pracovník – načti pozici, zobraz editační prvky s tlačítkem Uložit, skryj texty, zobraz telefon
                        var pracovnik = await db.Pracovnik
                            .Include(p => p.Pozice)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.IdStravnik == stravnik.IdStravnik);

                        PositionClass = pracovnik != null && pracovnik.Pozice != null ? $"Pozice: {pracovnik.Pozice.Nazev}" : "-";

                        ComboAlergiesVisibility = Visibility.Visible;
                        ComboDietRestrictionsVisibility = Visibility.Visible;
                        SaveButtonVisibility = Visibility.Visible;
                        TextAlergiesVisibility = Visibility.Collapsed;
                        TextDietRestrictionsVisibility = Visibility.Collapsed;

                        // Naplnění checkboxů – alergie a diety
                        SelectableAlergies.Clear();
                        var allAlergies = await db.Alergie.ToListAsync();
                        var selectedAlergieIds = await db.StravnikAlergie.Where(sa => sa.IdStravnik == stravnik.IdStravnik).Select(sa => sa.IdAlergie).ToListAsync();
                        foreach (var a in allAlergies)
                        {
                            SelectableAlergies.Add(new SelectableAlergie { Alergie = a, IsSelected = selectedAlergieIds.Contains(a.IdAlergie) });
                        }

                        SelectableDiets.Clear();
                        var allDiets = await db.DietniOmezeni.ToListAsync();
                        var selectedDietIds = await db.StravnikOmezeni.Where(so => so.IdStravnik == stravnik.IdStravnik).Select(so => so.IdOmezeni).ToListAsync();
                        foreach (var d in allDiets)
                        {
                            SelectableDiets.Add(new SelectableDiet { Diet = d, IsSelected = selectedDietIds.Contains(d.IdOmezeni) });
                        }

                        // Telefon – prefix +420, pokud je uložen
                        if (pracovnik != null && pracovnik.Telefon != 0)
                        {
                            Phone = $"+420{pracovnik.Telefon}";
                            PhoneVisibility = Visibility.Visible;
                        }
                        else
                        {
                            Phone = string.Empty;
                            PhoneVisibility = Visibility.Collapsed;
                        }

                        RokVisibility = Visibility.Collapsed;
                    }
                }
                catch (Exception ex)
                {
                    // Chyby ve větvi specifik (student/pracovník) neblokují zbytek – UI se nastaví do bezpečného stavu
                    System.Diagnostics.Debug.WriteLine("Load Student/Pracovnik failed: " + ex);
                    PositionClass = "-";
                    ComboAlergiesVisibility = Visibility.Collapsed;
                    ComboDietRestrictionsVisibility = Visibility.Collapsed;
                    SaveButtonVisibility = Visibility.Collapsed;
                    RokVisibility = Visibility.Collapsed;
                    PhoneVisibility = Visibility.Collapsed;
                }

                // 7) Profilová fotka – nejprve zkus soubor, jinak vygeneruj placeholder s iniciálami
                try
                {
                    try
                    {
                        // Kandidáti: soubory se stejným IdZaznam (preferujeme Tabulka=STRAVNICI), seřazení dle data nahrání
                        var candidates = await db.Soubor
                            .AsNoTracking()
                            .Where(s => s.IdZaznam == stravnik.IdStravnik)
                            .OrderByDescending(s => s.DatumNahrani)
                            .ToListAsync();

                        var photo = candidates
                            .FirstOrDefault(s => string.Equals((s.Tabulka ?? string.Empty).Trim(), "STRAVNICI", StringComparison.OrdinalIgnoreCase))
                            ?? candidates.FirstOrDefault();

                        // Fallback: vyhledat podle IdStravnik
                        if (photo == null)
                        {
                            photo = await db.Soubor
                                .AsNoTracking()
                                .Where(s => s.IdStravnik == stravnik.IdStravnik)
                                .OrderByDescending(s => s.DatumNahrani)
                                .FirstOrDefaultAsync();
                        }

                        if (photo != null && photo.Obsah != null && photo.Obsah.Length > 0)
                        {
                            using var ms = new MemoryStream(photo.Obsah);
                            var bmp = new BitmapImage();
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = ms;
                            bmp.EndInit();
                            bmp.Freeze();
                            ProfileImage = bmp;
                        }
                        else
                        {
                            ProfileImage = CreateInitialsImage(stravnik.Jmeno, stravnik.Prijmeni);
                        }
                    }
                    catch
                    {
                        ProfileImage = CreateInitialsImage(stravnik.Jmeno, stravnik.Prijmeni);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("CreateInitialsImage failed: " + ex);
                    ProfileImage = null;
                }
            }
            catch (Exception ex)
            {
                // Neočekávaná chyba – zalogovat, UI se pokusí zobrazit dostupná data
                System.Diagnostics.Debug.WriteLine("UserProfileViewModel.LoadAsync unexpected exception: " + ex);
            }
            finally
            {
                // Notifikace pro View – může skrýt spinner nebo povolit tlačítka
                try { LoadFinished?.Invoke(); } catch { }
            }
        }

        // Vytvoří placeholder obrázek s iniciálami – běží bezpečně na UI vlákně (přes Dispatcher)
        private ImageSource? CreateInitialsImage(string firstName, string lastName)
        {
            try
            {
                if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    return Application.Current.Dispatcher.Invoke(() => CreateInitialsImageCore(firstName, lastName));
                }
                return CreateInitialsImageCore(firstName, lastName);
            }
            catch
            {
                return null;
            }
        }

        // Jádro generování obrázku s iniciálami – 120x120 px, bílý podklad, font Segoe UI 48
        private ImageSource? CreateInitialsImageCore(string firstName, string lastName)
        {
            try
            {
                var initials = "";
                if (!string.IsNullOrWhiteSpace(firstName)) initials += firstName[0];
                if (!string.IsNullOrWhiteSpace(lastName)) initials += lastName[0];
                initials = initials.ToUpperInvariant();

                var dpi = 96;
                var width = 120;
                var height = 120;

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new Rect(0, 0, width, height), 15, 15);

                    var ft = new FormattedText(initials,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        48,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                    var pt = new Point((width - ft.Width) / 2, (height - ft.Height) / 2);
                    dc.DrawText(ft, pt);
                }

                var rtb = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
                rtb.Render(visual);
                return rtb;
            }
            catch
            {
                return null;
            }
        }

        // Ověření existence tabulky v aktuální DB schématu – dotazuje se USER_TABLES
        private bool TableExists(AppDbContext db, string tableName)
        {
            try
            {
                var conn = db.Database.GetDbConnection();
                var wasClosed = conn.State == System.Data.ConnectionState.Closed;
                if (wasClosed) conn.Open();
                using var cmd = conn.CreateCommand();
                // USER_TABLES obsahuje seznam tabulek přístupných aktuálnímu uživateli
                cmd.CommandText = $"SELECT COUNT(*) FROM user_tables WHERE table_name = '{tableName.ToUpperInvariant()}'";
                var obj = cmd.ExecuteScalar();
                if (wasClosed) conn.Close();
                if (obj == null) return false;
                if (int.TryParse(obj.ToString(), out var c)) return c > 0;
                if (long.TryParse(obj.ToString(), out var l)) return l > 0;
                return false;
            }
            catch
            {
                return false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

            // Zajistí, že PropertyChanged se vyvolá na UI vlákně (Dispatcher), aby se WPF bindingy korektně aktualizovaly
            try
            {
                if (Application.Current == null || Application.Current.Dispatcher.CheckAccess())
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => handler(this, new PropertyChangedEventArgs(name)));
                }
            }
            catch
            {
                // Ignorovat chyby při vyvolání PropertyChanged
            }
        }

        // Uloží změny (alergie a dietní omezení) vybraného uživatele – přepíše vazební tabulky podle aktuálního výběru
        public async Task SaveChangesAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var stravnik = await db.Stravnik.FirstOrDefaultAsync(s => s.Email == Email);
                if (stravnik == null)
                {
                    MessageBox.Show("Uživatel nenalezen.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ověření existence nutných tabulek
                bool hasAlergieTable = TableExists(db, "ALERGIE");
                bool hasAlergieStravniciTable = TableExists(db, "STRAVNICI_ALERGIE");
                bool hasDietTable = TableExists(db, "DIETNI_OMEZENI");
                bool hasOmezeniStravnikTable = TableExists(db, "STRAVNICI_OMEZENI");

                if (!hasAlergieTable || !hasAlergieStravniciTable || !hasDietTable || !hasOmezeniStravnikTable)
                {
                    MessageBox.Show("Některé tabulky neexistují.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Přepsání vazeb – nejprve odstraníme existující, poté přidáme aktuálně vybraný seznam
                var existingAlergies = await db.StravnikAlergie.Where(sa => sa.IdStravnik == stravnik.IdStravnik).ToListAsync();
                db.StravnikAlergie.RemoveRange(existingAlergies);
                var toAddAlergies = SelectableAlergies.Where(s => s.IsSelected && s.Alergie.IdAlergie != null).ToList();
                foreach (var sa in toAddAlergies)
                {
                    db.StravnikAlergie.Add(new StravnikAlergie { IdStravnik = stravnik.IdStravnik, IdAlergie = sa.Alergie.IdAlergie });
                }

                var existingDiets = await db.StravnikOmezeni.Where(so => so.IdStravnik == stravnik.IdStravnik).ToListAsync();
                db.StravnikOmezeni.RemoveRange(existingDiets);
                var toAddDiets = SelectableDiets.Where(s => s.IsSelected && s.Diet.IdOmezeni != null).ToList();
                foreach (var sd in toAddDiets)
                {
                    db.StravnikOmezeni.Add(new StravnikOmezeni { IdStravnik = stravnik.IdStravnik, IdOmezeni = sd.Diet.IdOmezeni });
                }

                await db.SaveChangesAsync();
                MessageBox.Show("Změny byly uloženy.", "Úspěch", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No inner exception";
                MessageBox.Show("Chyba při ukládání změn: " + ex.Message + "\nInner: " + inner, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Změna profilové fotografie – uloží soubor do DB (tabulka SOUBORY) a aktualizuje UI
        public async Task ChangePhotoAsync()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Vyberte fotografii",
                    Filter = "Obrázky|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var filePath = openFileDialog.FileName;
                    var bytes = await File.ReadAllBytesAsync(filePath);

                    using var db = new AppDbContext();
                    var stravnik = await db.Stravnik.FirstOrDefaultAsync(s => s.Email == Email);
                    if (stravnik == null) return;

                    // Vytvoření nového souboru (fotografie) v DB
                    var soubor = new Soubor
                    {
                        Nazev = Path.GetFileName(filePath),
                        Obsah = bytes,
                        DatumNahrani = DateTime.Now,
                        Tabulka = "STRAVNICI",
                        IdZaznam = stravnik.IdStravnik
                    };
                    db.Soubor.Add(soubor);
                    await db.SaveChangesAsync();

                    // Aktualizace UI – načtení z právě nahraných dat
                    using var ms = new MemoryStream(bytes);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();
                    ProfileImage = bmp;

                    MessageBox.Show("Fotografie byla změněna.", "Úspěch", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba při změně fotografie: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
