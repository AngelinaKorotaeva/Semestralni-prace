using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media.TextFormatting;
using System.Collections.Generic;
using System.IO;
using System.Data.Common;
using System.Collections.ObjectModel;

namespace SkolniJidelna.ViewModels
{
    public class UserProfileViewModel : INotifyPropertyChanged
    {
        private string _fullName = string.Empty;
        private string _balanceFormatted = "0 Kč";
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _address = string.Empty;
        private string _status = string.Empty;
        private string _positionClass = string.Empty;
        private string _rokNarozeni = string.Empty;
        private ImageSource? _profileImage;

        private Visibility _comboAlergiesVisibility = Visibility.Collapsed;
        private Visibility _comboDietRestrictionsVisibility = Visibility.Collapsed;
        private Visibility _saveButtonVisibility = Visibility.Collapsed;
        private Visibility _textAlergiesVisibility = Visibility.Visible;
        private Visibility _textDietRestrictionsVisibility = Visibility.Visible;
        private string _alergiesText = string.Empty;
        private string _dietRestrictionsText = string.Empty;
        private Visibility _rokVisibility = Visibility.Collapsed;
        private Visibility _phoneVisibility = Visibility.Collapsed;

        public string FullName { get => _fullName; private set { if (_fullName == value) return; _fullName = value; OnPropertyChanged(nameof(FullName)); } }
        public string BalanceFormatted { get => _balanceFormatted; private set { if (_balanceFormatted == value) return; _balanceFormatted = value; OnPropertyChanged(nameof(BalanceFormatted)); } }
        public string Email { get => _email; private set { if (_email == value) return; _email = value; OnPropertyChanged(nameof(Email)); } }
        public string Phone { get => _phone; private set { if (_phone == value) return; _phone = value; OnPropertyChanged(nameof(Phone)); } }
        public string Address { get => _address; private set { if (_address == value) return; _address = value; OnPropertyChanged(nameof(Address)); } }
        public string Status { get => _status; private set { if (_status == value) return; _status = value; OnPropertyChanged(nameof(Status)); } }
        public string PositionClass { get => _positionClass; private set { if (_positionClass == value) return; _positionClass = value; OnPropertyChanged(nameof(PositionClass)); } }
        public string RokNarozeni { get => _rokNarozeni; private set { if (_rokNarozeni == value) return; _rokNarozeni = value; OnPropertyChanged(nameof(RokNarozeni)); } }

        public ImageSource? ProfileImage { get => _profileImage; private set { if (_profileImage == value) return; _profileImage = value; OnPropertyChanged(nameof(ProfileImage)); } }

        public Visibility ComboAlergiesVisibility { get => _comboAlergiesVisibility; private set { if (_comboAlergiesVisibility == value) return; _comboAlergiesVisibility = value; OnPropertyChanged(nameof(ComboAlergiesVisibility)); } }
        public Visibility ComboDietRestrictionsVisibility { get => _comboDietRestrictionsVisibility; private set { if (_comboDietRestrictionsVisibility == value) return; _comboDietRestrictionsVisibility = value; OnPropertyChanged(nameof(ComboDietRestrictionsVisibility)); } }
        public Visibility SaveButtonVisibility { get => _saveButtonVisibility; private set { if (_saveButtonVisibility == value) return; _saveButtonVisibility = value; OnPropertyChanged(nameof(SaveButtonVisibility)); } }
        public Visibility TextAlergiesVisibility { get => _textAlergiesVisibility; private set { if (_textAlergiesVisibility == value) return; _textAlergiesVisibility = value; OnPropertyChanged(nameof(TextAlergiesVisibility)); } }
        public Visibility TextDietRestrictionsVisibility { get => _textDietRestrictionsVisibility; private set { if (_textDietRestrictionsVisibility == value) return; _textDietRestrictionsVisibility = value; OnPropertyChanged(nameof(TextDietRestrictionsVisibility)); } }
        public string AlergiesText { get => _alergiesText; private set { if (_alergiesText == value) return; _alergiesText = value; OnPropertyChanged(nameof(AlergiesText)); } }
        public string DietRestrictionsText { get => _dietRestrictionsText; private set { if (_dietRestrictionsText == value) return; _dietRestrictionsText = value; OnPropertyChanged(nameof(DietRestrictionsText)); } }
        public Visibility RokVisibility { get => _rokVisibility; private set { if (_rokVisibility == value) return; _rokVisibility = value; OnPropertyChanged(nameof(RokVisibility)); } }
        public Visibility PhoneVisibility { get => _phoneVisibility; private set { if (_phoneVisibility == value) return; _phoneVisibility = value; OnPropertyChanged(nameof(PhoneVisibility)); } }

        public ObservableCollection<Alergie> AvailableAlergies { get; } = new();
        public ObservableCollection<Alergie> SelectedAlergies { get; } = new();
        public ObservableCollection<DietniOmezeni> AvailableDietRestrictions { get; } = new();
        public ObservableCollection<DietniOmezeni> SelectedDietRestrictions { get; } = new();

        public ObservableCollection<SelectableAlergie> SelectableAlergies { get; } = new();
        public ObservableCollection<SelectableDiet> SelectableDiets { get; } = new();

        // Event to signal that async loading finished
        public event Action? LoadFinished;

        public UserProfileViewModel() { }

        public UserProfileViewModel(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
            // fire-and-forget load on UI thread
            _ = LoadAsync(email.Trim());
        }

        private async Task LoadAsync(string email)
        {
            try
            {
                using var db = new AppDbContext();

                // Load main entity first (simpler SQL)
                var stravnik = await db.Stravnik
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Email == email);

                if (stravnik == null)
                {
                    Email = email;
                    FullName = "U?ivatel nenalezen";
                    return;
                }

                Email = stravnik.Email;
                FullName = $"{stravnik.Jmeno} {stravnik.Prijmeni}";
                BalanceFormatted = string.Format("{0:0.##} Kč", stravnik.Zustatek);

                // map TypStravnik into localized status
                var t = (stravnik.TypStravnik ?? string.Empty).Trim().ToLowerInvariant();
                if (t == "pr") Status = "pracovnik";
                else if (t == "st") Status = "student";
                else Status = stravnik.TypStravnik ?? string.Empty;

                // Address (load separately)
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
                    System.Diagnostics.Debug.WriteLine("Load Address failed: " + ex);
                    Address = string.Empty;
                }

                // Allergies (join explicitly to avoid complex single SQL with many LEFT JOINs)
                try
                {
                    // Check that underlying tables exist for current DB user to avoid ORA-00942
                    bool hasAlergieTable = TableExists(db, "ALERGIE");
                    bool hasAlergieStravniciTable = TableExists(db, "STRAVNICI_ALERGIE");
                    if (!hasAlergieTable || !hasAlergieStravniciTable)
                    {
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

                        AlergiesText = allergyNames.Any() ? string.Join(", ", allergyNames) : string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Load Allergies failed: " + ex);
                    AlergiesText = string.Empty;
                }

                // Diet restrictions
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

                // Load student or pracovnik info separately
                try
                {
                    var typ = (stravnik.TypStravnik ?? string.Empty).Trim().ToLowerInvariant();
                    if (typ == "st" || string.Equals(stravnik.TypStravnik, "student", StringComparison.OrdinalIgnoreCase))
                    {
                        var student = await db.Student
                            .Include(t2 => t2.Trida)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(s => s.IdStravnik == stravnik.IdStravnik);

                        PositionClass = student != null && student.Trida != null ? $"Třída: {student.Trida.CisloTridy}" : "-";

                        // Show selectable lists for student too
                        ComboAlergiesVisibility = Visibility.Visible;
                        ComboDietRestrictionsVisibility = Visibility.Visible;
                        SaveButtonVisibility = Visibility.Collapsed; // keep saving disabled for students unless needed
                        TextAlergiesVisibility = Visibility.Collapsed;
                        TextDietRestrictionsVisibility = Visibility.Collapsed;

                        // Load selectable allergies
                        SelectableAlergies.Clear();
                        var allAlergies = await db.Alergie.ToListAsync();
                        var selectedAlergieIds = await db.StravnikAlergie.Where(sa => sa.IdStravnik == stravnik.IdStravnik).Select(sa => sa.IdAlergie).ToListAsync();
                        foreach (var a in allAlergies)
                        {
                            SelectableAlergies.Add(new SelectableAlergie { Alergie = a, IsSelected = selectedAlergieIds.Contains(a.IdAlergie) });
                        }

                        // Load selectable diet restrictions
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

                        // Load selectable allergies
                        SelectableAlergies.Clear();
                        var allAlergies = await db.Alergie.ToListAsync();
                        var selectedAlergieIds = await db.StravnikAlergie.Where(sa => sa.IdStravnik == stravnik.IdStravnik).Select(sa => sa.IdAlergie).ToListAsync();
                        foreach (var a in allAlergies)
                        {
                            SelectableAlergies.Add(new SelectableAlergie { Alergie = a, IsSelected = selectedAlergieIds.Contains(a.IdAlergie) });
                        }

                        // Load selectable diet restrictions
                        SelectableDiets.Clear();
                        var allDiets = await db.DietniOmezeni.ToListAsync();
                        var selectedDietIds = await db.StravnikOmezeni.Where(so => so.IdStravnik == stravnik.IdStravnik).Select(so => so.IdOmezeni).ToListAsync();
                        foreach (var d in allDiets)
                        {
                            SelectableDiets.Add(new SelectableDiet { Diet = d, IsSelected = selectedDietIds.Contains(d.IdOmezeni) });
                        }

                        // prefix phone with +420 when present
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
                    System.Diagnostics.Debug.WriteLine("Load Student/Pracovnik failed: " + ex);
                    PositionClass = "-";
                    ComboAlergiesVisibility = Visibility.Collapsed;
                    ComboDietRestrictionsVisibility = Visibility.Collapsed;
                    SaveButtonVisibility = Visibility.Collapsed;
                    RokVisibility = Visibility.Collapsed;
                    PhoneVisibility = Visibility.Collapsed;
                }

                // Profile image placeholder (initials)
                try
                {
                    try
                    {
                        // 1) Try by ID_ZAZNAM on server, then filter TABULKA on client to avoid Oracle function/literal issues
                        var candidates = await db.Soubor
                            .AsNoTracking()
                            .Where(s => s.IdZaznam == stravnik.IdStravnik)
                            .OrderByDescending(s => s.DatumNahrani)
                            .ToListAsync();

                        var photo = candidates
                            .FirstOrDefault(s => string.Equals((s.Tabulka ?? string.Empty).Trim(), "STRAVNICI", StringComparison.OrdinalIgnoreCase))
                            ?? candidates.FirstOrDefault();

                        // 2) Fallback: try by ID_STRAVNIK column if nothing found
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
                // unexpected top-level error
                System.Diagnostics.Debug.WriteLine("UserProfileViewModel.LoadAsync unexpected exception: " + ex);
            }
            finally
            {
                // notify view that loading finished (success or failure)
                try { LoadFinished?.Invoke(); } catch { }
            }
        }

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

        private bool TableExists(AppDbContext db, string tableName)
        {
            try
            {
                var conn = db.Database.GetDbConnection();
                var wasClosed = conn.State == System.Data.ConnectionState.Closed;
                if (wasClosed) conn.Open();
                using var cmd = conn.CreateCommand();
                // use USER_TABLES which lists tables accessible to current schema
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

            // Ensure the PropertyChanged event is raised on the UI thread so WPF bindings update correctly
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
                // ignore exceptions from raising PropertyChanged
            }
        }

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

                // Check tables exist
                bool hasAlergieTable = TableExists(db, "ALERGIE");
                bool hasAlergieStravniciTable = TableExists(db, "STRAVNICI_ALERGIE");
                bool hasDietTable = TableExists(db, "DIETNI_OMEZENI");
                bool hasOmezeniStravnikTable = TableExists(db, "STRAVNICI_OMEZENI");

                if (!hasAlergieTable || !hasAlergieStravniciTable || !hasDietTable || !hasOmezeniStravnikTable)
                {
                    MessageBox.Show("Některé tabulky neexistují.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Debug: show counts
                int selectedAlergiesCount = SelectableAlergies.Count(s => s.IsSelected);
                int selectedDietsCount = SelectableDiets.Count(s => s.IsSelected);
                MessageBox.Show($"Selected allergies: {selectedAlergiesCount}, Selected diets: {selectedDietsCount}", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);

                // Save allergies
                var existingAlergies = await db.StravnikAlergie.Where(sa => sa.IdStravnik == stravnik.IdStravnik).ToListAsync();
                db.StravnikAlergie.RemoveRange(existingAlergies);
                var toAddAlergies = SelectableAlergies.Where(s => s.IsSelected && s.Alergie.IdAlergie != null).ToList();
                MessageBox.Show($"Will add {toAddAlergies.Count} allergies: {string.Join(", ", toAddAlergies.Select(a => a.Alergie.IdAlergie))}", "Debug Add", MessageBoxButton.OK, MessageBoxImage.Information);
                foreach (var sa in toAddAlergies)
                {
                    db.StravnikAlergie.Add(new StravnikAlergie { IdStravnik = stravnik.IdStravnik, IdAlergie = sa.Alergie.IdAlergie });
                }
                MessageBox.Show($"Context has {db.StravnikAlergie.Local.Count} StravnikAlergie entities", "Context", MessageBoxButton.OK, MessageBoxImage.Information);

                // Save diet restrictions
                var existingDiets = await db.StravnikOmezeni.Where(so => so.IdStravnik == stravnik.IdStravnik).ToListAsync();
                db.StravnikOmezeni.RemoveRange(existingDiets);
                var toAddDiets = SelectableDiets.Where(s => s.IsSelected && s.Diet.IdOmezeni != null).ToList();
                MessageBox.Show($"Will add {toAddDiets.Count} diets: {string.Join(", ", toAddDiets.Select(d => d.Diet.IdOmezeni))}", "Debug Add", MessageBoxButton.OK, MessageBoxImage.Information);
                foreach (var sd in toAddDiets)
                {
                    db.StravnikOmezeni.Add(new StravnikOmezeni { IdStravnik = stravnik.IdStravnik, IdOmezeni = sd.Diet.IdOmezeni });
                }
                MessageBox.Show($"Context has {db.StravnikOmezeni.Local.Count} StravnikOmezeni entities", "Context", MessageBoxButton.OK, MessageBoxImage.Information);

                await db.SaveChangesAsync();
                MessageBox.Show("Změny byly uloženy.", "Úspěch", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "No inner exception";
                MessageBox.Show("Chyba při ukládání změn: " + ex.Message + "\nInner: " + inner, "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
