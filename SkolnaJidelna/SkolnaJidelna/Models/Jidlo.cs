using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    public class Jidlo
    {
        public int IdJidlo { get; set; }
        public string Nazev { get; set; } = null!;
        public string Popis { get; set; } = null!;
        public string Kategorie { get; set; } = null!;
        public double Cena { get; set; }
        public string? Poznamka { get; set; }

        public int? IdMenu { get; set; }
        public Menu? Menu { get; set; }

        public ICollection<Polozka>? Polozky { get; set; }
        public ICollection<SlozkaJidlo>? SlozkyJidla { get; set; }
    }
}
