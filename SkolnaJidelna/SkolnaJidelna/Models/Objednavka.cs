using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    public class Objednavka
    {
        public int IdObjednavka { get; set; }
        public DateTime Datum { get; set; }
        public double CelkovaCena { get; set; }
        public string? Poznamka { get; set; }

        public int IdStav { get; set; }
        public Stav Stav { get; set; } = null!;

        public int IdStravnik { get; set; }
        public Stravnik Stravnik { get; set; } = null!;

        public ICollection<Polozka>? Polozky { get; set; }
    }
}
