using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using SkolniJidelna.ViewModels;
using SkolniJidelna.ViewModels.SkolniJidelna.ViewModels;

namespace SkolniJidelna.ViewModels
{
    public class AdminProfileViewModel : INotifyPropertyChanged
    {
        private string _fullName = string.Empty;
        private string _role = string.Empty;
        private string _balanceFormatted = "0 Kč";
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _address = string.Empty;
        private string _status = string.Empty;
        private string _positionClass = string.Empty;
        private ImageSource? _profileImage;

        public string FullName { get => _fullName; private set { if (_fullName == value) return; _fullName = value; OnPropertyChanged(nameof(FullName)); } }
        public string Role { get => _role; private set { if (_role == value) return; _role = value; OnPropertyChanged(nameof(Role)); } }
        public string BalanceFormatted { get => _balanceFormatted; private set { if (_balanceFormatted == value) return; _balanceFormatted = value; OnPropertyChanged(nameof(BalanceFormatted)); } }
        public string Email { get => _email; private set { if (_email == value) return; _email = value; OnPropertyChanged(nameof(Email)); } }
        public string Phone { get => _phone; private set { if (_phone == value) return; _phone = value; OnPropertyChanged(nameof(Phone)); } }
        public string Address { get => _address; private set { if (_address == value) return; _address = value; OnPropertyChanged(nameof(Address)); } }
        public string Status { get => _status; private set { if (_status == value) return; _status = value; OnPropertyChanged(nameof(Status)); } }
        public string PositionClass { get => _positionClass; private set { if (_positionClass == value) return; _positionClass = value; OnPropertyChanged(nameof(PositionClass)); } }

        public ImageSource? ProfileImage
        {
            get => _profileImage;
            private set { if (_profileImage == value) return; _profileImage = value; OnPropertyChanged(nameof(ProfileImage)); }
        }

        public ObservableCollection<EditableProperty> Properties { get; } = new();

        public ObservableCollection<SelectableAlergie> SelectableAlergies { get; } = new();
        public ObservableCollection<SelectableDiet> SelectableDiets { get; } = new();

        private Visibility _comboAlergiesVisibility = Visibility.Collapsed;
        private Visibility _comboDietRestrictionsVisibility = Visibility.Collapsed;
        private Visibility _textAlergiesVisibility = Visibility.Visible;
        private Visibility _textDietRestrictionsVisibility = Visibility.Visible;
        private Visibility _saveButtonVisibility = Visibility.Collapsed;

        public Visibility ComboAlergiesVisibility { get => _comboAlergiesVisibility; private set { if (_comboAlergiesVisibility == value) return; _comboAlergiesVisibility = value; OnPropertyChanged(nameof(ComboAlergiesVisibility)); } }
        public Visibility ComboDietRestrictionsVisibility { get => _comboDietRestrictionsVisibility; private set { if (_comboDietRestrictionsVisibility == value) return; _comboDietRestrictionsVisibility = value; OnPropertyChanged(nameof(ComboDietRestrictionsVisibility)); } }
        public Visibility TextAlergiesVisibility { get => _textAlergiesVisibility; private set { if (_textAlergiesVisibility == value) return; _textAlergiesVisibility = value; OnPropertyChanged(nameof(TextAlergiesVisibility)); } }
        public Visibility TextDietRestrictionsVisibility { get => _textDietRestrictionsVisibility; private set { if (_textDietRestrictionsVisibility == value) return; _textDietRestrictionsVisibility = value; OnPropertyChanged(nameof(TextDietRestrictionsVisibility)); } }
        public Visibility SaveButtonVisibility { get => _saveButtonVisibility; private set { if (_saveButtonVisibility == value) return; _saveButtonVisibility = value; OnPropertyChanged(nameof(SaveButtonVisibility)); } }

        private object? _selectedItem;
        public object? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem == value) return;
                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
                LoadPropertiesForSelectedItem();
            }
        }

        // Připraví editovatelné vlastnosti aktuálně vybrané entity.
        private void LoadPropertiesForSelectedItem()
        {
            Properties.Clear();
            var obj = SelectedItem;
            if (obj == null) return;
            var props = obj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                           .Where(p => p.CanRead && p.GetMethod != null)
                           .Where(p => IsScalarType(p.PropertyType));
            foreach (var p in props)
            {
                var val = p.GetValue(obj);
                Properties.Add(new EditableProperty(p.Name, p.PropertyType, val));
            }
        }

        // Pro nový záznam: vytvoří prázdné položky Properties pro daný typ.
        public void PopulateEmptyPropertiesForType(Type entityClrType)
        {
            if (entityClrType == null) return;
            Properties.Clear();
            var props = entityClrType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => IsScalarType(p.PropertyType));
            foreach (var p in props)
            {
                Properties.Add(new EditableProperty(p.Name, p.PropertyType, null));
            }
        }

        // Vytvoří instanci daného typu, naplní ji z Properties a uloží do DB.
        public bool SaveNewEntity(Type entityClrType)
        {
            if (entityClrType == null) return false;
            try
            {
                var entity = Activator.CreateInstance(entityClrType);
                if (entity == null) return false;

                foreach (var ep in Properties)
                {
                    var prop = entityClrType.GetProperty(ep.Name);
                    if (prop == null || !prop.CanWrite) continue;
                    var valueToSet = ep.Value;
                    if (valueToSet != null && !prop.PropertyType.IsAssignableFrom(valueToSet.GetType()))
                    {
                        valueToSet = Convert.ChangeType(valueToSet, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType, CultureInfo.InvariantCulture);
                    }
                    prop.SetValue(entity, valueToSet);
                }

                using var ctx = new AppDbContext();
                ctx.Add(entity);
                ctx.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Rozhodne, zda typ je „jednoduchý“ (string, číslo, enum, DateTime); kolekce/navigace vrací false.
        private static bool IsScalarType(Type t)
        {
            if (t == typeof(string)) return true;
            if (t.IsPrimitive || t.IsEnum) return true;
            if (t == typeof(DateTime) || t == typeof(decimal) || t == typeof(double) || t == typeof(float)) return true;
            var nt = Nullable.GetUnderlyingType(t);
            if (nt != null) return IsScalarType(nt);

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t != typeof(string)) return false;
            return false;
        }

        // Přepíše hodnoty zpět do SelectedItem a uloží je do DB jako Modified.
        private void SaveSelectedItem()
        {
            if (SelectedItem == null) return;
            var obj = SelectedItem;
            var type = obj.GetType();
            foreach (var ep in Properties)
            {
                var prop = type.GetProperty(ep.Name);
                if (prop == null || !prop.CanWrite) continue;
                try
                {
                    var valueToSet = ep.Value;
                    if (valueToSet != null && !prop.PropertyType.IsAssignableFrom(valueToSet.GetType()))
                    {
                        valueToSet = Convert.ChangeType(valueToSet, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    prop.SetValue(obj, valueToSet);
                }
                catch
                {
                }
            }

            try
            {
                using var ctx = new AppDbContext();
                ctx.Attach(obj);
                ctx.Entry(obj).State = EntityState.Modified;
                ctx.SaveChanges();
            }
            catch (Exception ex)
            {
            }
        }

        // Konstruktor: načte profil podle emailu hned po vytvoření.
        public AdminProfileViewModel(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
            LoadByEmail(email.Trim());
        }

        // Načte uživatele podle emailu, vyplní základní údaje, alergie/diety, pozici/třídu a fotku.
        private void LoadByEmail(string email)
        {
            try
            {
                using var ctx = new AppDbContext();
                var stravnik = ctx.Stravnik
                .Include(s => s.Adresa)
                .AsNoTracking()
                .FirstOrDefault(s => s.Email == email);

                if (stravnik == null)
                {
                    Email = email;
                    FullName = "Uživatel nenalezen";
                    return;
                }

                Email = stravnik.Email;
                FullName = $"{stravnik.Jmeno} {stravnik.Prijmeni}";
                Role = stravnik.Role?.Trim() ?? string.Empty;
                BalanceFormatted = string.Format("{0:0.##} Kč", stravnik.Zustatek);
                var t = (stravnik.TypStravnik ?? string.Empty).Trim().ToLowerInvariant();
                if (t == "pr") Status = "pracovnik";
                else if (t == "st") Status = "student";
                else Status = stravnik.TypStravnik ?? string.Empty;

                if (stravnik.Adresa != null)
                {
                    Address = $"{stravnik.Adresa.Psc} {stravnik.Adresa.Ulice}, {stravnik.Adresa.Mesto}".Trim();
                }

                var prac = ctx.Pracovnik.Find(stravnik.IdStravnik);

                if (prac != null)
                {
                    Phone = prac.Telefon != 0 ? $"+420{prac.Telefon}" : string.Empty;
                    var poz = ctx.Pozice.Find(prac.IdPozice);
                    PositionClass = poz != null ? poz.Nazev : prac.IdPozice.ToString();

                    SelectableAlergies.Clear();
                    var allAlergies = ctx.Alergie.ToList();
                    var selectedAlergieIds = ctx.StravnikAlergie.Where(sa => sa.IdStravnik == stravnik.IdStravnik).Select(sa => sa.IdAlergie).ToList();
                    foreach (var a in allAlergies)
                    {
                        SelectableAlergies.Add(new SelectableAlergie { Alergie = a, IsSelected = selectedAlergieIds.Contains(a.IdAlergie) });
                    }

                    SelectableDiets.Clear();
                    var allDiets = ctx.DietniOmezeni.ToList();
                    var selectedDietIds = ctx.StravnikOmezeni.Where(so => so.IdStravnik == stravnik.IdStravnik).Select(so => so.IdOmezeni).ToList();
                    foreach (var d in allDiets)
                    {
                        SelectableDiets.Add(new SelectableDiet { Diet = d, IsSelected = selectedDietIds.Contains(d.IdOmezeni) });
                    }

                    ComboAlergiesVisibility = Visibility.Visible;
                    ComboDietRestrictionsVisibility = Visibility.Visible;
                    TextAlergiesVisibility = Visibility.Collapsed;
                    TextDietRestrictionsVisibility = Visibility.Collapsed;
                    SaveButtonVisibility = Visibility.Visible;
                }
                else
                {
                    var stud = ctx.Student.Find(stravnik.IdStravnik);
                    if (stud != null)
                    {
                        PositionClass = $"Třída {stud.IdTrida}";
                    }
                }

                try
                {
                    var candidates = ctx.Soubor
                        .AsNoTracking()
                        .Where(s => s.IdZaznam == stravnik.IdStravnik)
                        .OrderByDescending(s => s.DatumNahrani)
                        .ToList();

                    var photo = candidates
                        .FirstOrDefault(s => string.Equals((s.Tabulka ?? string.Empty).Trim(), "STRAVNICI", StringComparison.OrdinalIgnoreCase))
                        ?? candidates.FirstOrDefault();

                    if (photo == null)
                    {
                        photo = ctx.Soubor
                            .AsNoTracking()
                            .Where(s => s.IdStravnik == stravnik.IdStravnik)
                            .OrderByDescending(s => s.DatumNahrani)
                            .FirstOrDefault();
                    }

                    if (photo != null && photo.Obsah != null && photo.Obsah.Length > 0)
                    {
                        using var ms = new System.IO.MemoryStream(photo.Obsah);
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
            catch
            {
            }
        }

        // Fallback obrázek s iniciálami – na UI thread nebo pomocí Dispatcher.
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

        // Vykreslí 120x120 bitmapu s iniciálami (bílý podklad, černý text).
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
                    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new System.Windows.Rect(0, 0, width, height), 15, 15);

                    var ft = new FormattedText(initials,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    48,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                    var pt = new System.Windows.Point((width - ft.Width) / 2, (height - ft.Height) / 2);
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

        // Vyvolá PropertyChanged na správném vlákně, aby se zaktualizovaly bindingy.
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler == null) return;

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
            }
        }

        // Zapíše vybrané alergie/dietní omezení pro aktuální Email do DB (předem kontroluje existenci tabulek).
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

                bool hasAlergieTable = TableExists(db, "ALERGIE");
                bool hasAlergieStravniciTable = TableExists(db, "STRAVNICI_ALERGIE");
                bool hasDietTable = TableExists(db, "DIETNI_OMEZENI");
                bool hasOmezeniStravnikTable = TableExists(db, "STRAVNICI_OMEZENI");

                if (!hasAlergieTable || !hasAlergieStravniciTable || !hasDietTable || !hasOmezeniStravnikTable)
                {
                    MessageBox.Show("Některé tabulky neexistují.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var existingAlergies = await db.StravnikAlergie.Where(sa => sa.IdStravnik == stravnik.IdStravnik).ToListAsync();
                db.StravnikAlergie.RemoveRange(existingAlergies);
                foreach (var sa in SelectableAlergies.Where(s => s.IsSelected && s.Alergie.IdAlergie != null))
                {
                    db.StravnikAlergie.Add(new StravnikAlergie { IdStravnik = stravnik.IdStravnik, IdAlergie = sa.Alergie.IdAlergie });
                }

                var existingDiets = await db.StravnikOmezeni.Where(so => so.IdStravnik == stravnik.IdStravnik).ToListAsync();
                db.StravnikOmezeni.RemoveRange(existingDiets);
                foreach (var sd in SelectableDiets.Where(s => s.IsSelected && s.Diet.IdOmezeni != null))
                {
                    db.StravnikOmezeni.Add(new StravnikOmezeni { IdStravnik = stravnik.IdStravnik, IdOmezeni = sd.Diet.IdOmezeni });
                }

                await db.SaveChangesAsync();
                MessageBox.Show("Změny byly uloženy.", "Úspěch", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba při ukládání změn: " + ex.Message, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Zkontroluje v USER_TABLES, zda daná tabulka existuje (pro ochranu před ORA-00942).
        private bool TableExists(AppDbContext db, string tableName)
        {
            try
            {
                var conn = db.Database.GetDbConnection();
                var wasClosed = conn.State == System.Data.ConnectionState.Closed;
                if (wasClosed) conn.Open();
                using var cmd = conn.CreateCommand();
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

        // Vybere fotku z disku, uloží ji do SOUBORY a nastaví ji jako profilový náhled.
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
                    var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

                    using var db = new AppDbContext();
                    var stravnik = await db.Stravnik.FirstOrDefaultAsync(s => s.Email == Email);
                    if (stravnik == null) return;


                    var soubor = new Soubor
                    {
                        Nazev = System.IO.Path.GetFileName(filePath),
                        Obsah = bytes,
                        DatumNahrani = DateTime.Now,
                        Tabulka = "STRAVNICI",
                        IdZaznam = stravnik.IdStravnik
                    };
                    db.Soubor.Add(soubor);
                    await db.SaveChangesAsync();

                    using var ms = new System.IO.MemoryStream(bytes);
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
