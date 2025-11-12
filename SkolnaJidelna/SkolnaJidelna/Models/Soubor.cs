using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkolnaJidelna.Models;

namespace SkolniJidelna.Models
{
    public class Soubor
    {
        public int IdSoubor { get; set; }
        public string Nazev { get; set; } = null!;
        public string Typ { get; set; } = null!;
        public string Pripona { get; set; } = null!;
        public byte[] Obsah { get; set; } = null!;
        public DateTime DatumNahrani { get; set; }
        public DateTime? DatumModifikace { get; set; }
        public string? Operace { get; set; }
        public string Tabulka { get; set; } = null!;
        public int IdZaznam { get; set; }

        public int IdStravnik { get; set; }
        public Stravnik Stravnik { get; set; } = null!;
    }
}
