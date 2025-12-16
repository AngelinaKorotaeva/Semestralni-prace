using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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

        // New property for direct ImageSource binding
        public ImageSource? ProfileImage
        {
            get => _profileImage;
            private set { if (_profileImage == value) return; _profileImage = value; OnPropertyChanged(nameof(ProfileImage)); }
        }

        public ObservableCollection<EditableProperty> Properties { get; } = new();

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

        public bool SaveNewEntity(Type entityClrType)
        {
            if (entityClrType == null) return false;
            try
            {
                var entity = Activator.CreateInstance(entityClrType);
                if (entity == null) return false;

                // assign values from Properties
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
                ctx.Add(entity); // use non-generic Add(object) to avoid type inference issues
                ctx.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsScalarType(Type t)
        {
            if (t == typeof(string)) return true;
            if (t.IsPrimitive || t.IsEnum) return true;
            if (t == typeof(DateTime) || t == typeof(decimal) || t == typeof(double) || t == typeof(float)) return true;
            var nt = Nullable.GetUnderlyingType(t);
            if (nt != null) return IsScalarType(nt);
            // exclude collections and complex types
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t != typeof(string)) return false;
            return false;
        }

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
                    // убедиться, что Value имеет корректный тип
                    var valueToSet = ep.Value;
                    if (valueToSet != null && !prop.PropertyType.IsAssignableFrom(valueToSet.GetType()))
                    {
                        valueToSet = Convert.ChangeType(valueToSet, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    prop.SetValue(obj, valueToSet);
                }
                catch
                {
                    // обработка ошибок парсинга — можно показать сообщение
                }
            }

            // сохранить в БД (если объект — сущность EF)
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

        public AdminProfileViewModel() { }

        // Create and load by email (email should be unique)
        public AdminProfileViewModel(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
            LoadByEmail(email.Trim());
        }

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
                // Map typ_stravnik to localized status (keep original when unknown)
                var t = (stravnik.TypStravnik ?? string.Empty).Trim().ToLowerInvariant();
                if (t == "pr") Status = "pracovnik";
                else if (t == "st") Status = "student";
                else Status = stravnik.TypStravnik ?? string.Empty;

                // Address
                if (stravnik.Adresa != null)
                {
                    Address = $"{stravnik.Adresa.Psc} {stravnik.Adresa.Ulice}, {stravnik.Adresa.Mesto}".Trim();
                }

                // Try load pracovnik by primary key
                var prac = ctx.Pracovnik.Find(stravnik.IdStravnik);

                if (prac != null)
                {
                    // Prefix phone digits with country code +420 when present
                    Phone = prac.Telefon != 0 ? $"+420{prac.Telefon}" : string.Empty;
                    var poz = ctx.Pozice.Find(prac.IdPozice);
                    PositionClass = poz != null ? poz.Nazev : prac.IdPozice.ToString();
                }
                else
                {
                    // student
                    var stud = ctx.Student.Find(stravnik.IdStravnik);
                    if (stud != null)
                    {
                        PositionClass = $"Třída {stud.IdTrida}";
                    }
                }

                // Load profile image from SOUBORY table (latest by DATUM_NAHRANI); fallback to initials
                try
                {
                    var photo = ctx.Soubor
                        .AsNoTracking()
                        .Where(s => s.IdStravnik == stravnik.IdStravnik)
                        .OrderByDescending(s => s.DatumNahrani)
                        .FirstOrDefault();
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
                // swallow - UI will show empty/default values
            }
        }

        private ImageSource? CreateInitialsImage(string firstName, string lastName)
        {
            try
            {
                var initials = "";
                if (!string.IsNullOrWhiteSpace(firstName)) initials += firstName[0];
                if (!string.IsNullOrWhiteSpace(lastName)) initials += lastName[0];
                initials = initials.ToUpperInvariant();

                // Create a RenderTargetBitmap with text drawn
                var dpi = 96;
                var width = 120;
                var height = 120;

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    // background
                    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255, 255, 255)), null, new System.Windows.Rect(0, 0, width, height), 15, 15);

                    // text
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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
