using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;
using SkolniJidelna.Models;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkolniJidelna.ViewModels
{
    public class AdminProfileViewModel : INotifyPropertyChanged
    {
    private string _fullName = string.Empty;
    private string _balanceFormatted = "0 Kč";
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _address = string.Empty;
    private string _status = string.Empty;
    private string _positionClass = string.Empty;
    private ImageSource? _profileImage;

    public string FullName { get => _fullName; private set { if (_fullName == value) return; _fullName = value; OnPropertyChanged(nameof(FullName)); } }
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
    BalanceFormatted = string.Format("{0:0.##} Kč", stravnik.Zustatek);
    Status = stravnik.TypStravnik;

    // Address
    if (stravnik.Adresa != null)
    {
    Address = $"{stravnik.Adresa.Psc} {stravnik.Adresa.Ulice}, {stravnik.Adresa.Mesto}".Trim();
    }

    // Try load pracovnik by primary key
    var prac = ctx.Pracovnik.Find(stravnik.IdStravnik);

    if (prac != null)
    {
    Phone = prac.Telefon !=0 ? prac.Telefon.ToString() : string.Empty;
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

    // Profile image: not stored currently, create initials placeholder as DrawingImage
    ProfileImage = CreateInitialsImage(stravnik.Jmeno, stravnik.Prijmeni);
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
    var dpi =96;
    var width =120;
    var height =120;

    var visual = new DrawingVisual();
    using (var dc = visual.RenderOpen())
    {
    // background
    dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(255,255,255)), null, new System.Windows.Rect(0,0, width, height),15,15);

    // text
    var ft = new FormattedText(initials,
    System.Globalization.CultureInfo.InvariantCulture,
    System.Windows.FlowDirection.LeftToRight,
    new Typeface("Segoe UI"),
    48,
    Brushes.Black,
    VisualTreeHelper.GetDpi(visual).PixelsPerDip);

    var pt = new System.Windows.Point((width - ft.Width) /2, (height - ft.Height) /2);
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
