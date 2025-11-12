using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using SkolniJidelna.Models;

namespace SkolnaJidelna.Models
{
    public class Stravnik
    {
        public int IdStravnik { get; set; }
        public string Jmeno { get; set; } = null!;
        public string Prijmeni { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Heslo { get; set; }
        public double Zustatek { get; set; }
        public string Role { get; set; } = null!;
        public string Aktivita { get; set; } = null!;
        public string TypStravnik { get; set; } = null!;

        public int IdAdresa { get; set; }
        public Adresa Adresa { get; set; } = null!;

        public ICollection<Objednavka>? Objednavky { get; set; }
        public ICollection<Platba>? Platby { get; set; }
        public ICollection<Soubor>? Soubory { get; set; }
        public ICollection<StravnikAlergie>? Alergie { get; set; }
        public ICollection<StravnikOmezeni>? Omezeni { get; set; }
    }
}
