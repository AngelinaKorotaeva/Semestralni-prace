using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Polozka
    {
        public int IdJidlo { get; set; }
        public int IdObjednavka { get; set; }
        public int Mnozstvi { get; set; }
        public double CenaPolozky { get; set; }

        public Jidlo Jidlo { get; set; } = null!;
        public Objednavka Objednavka { get; set; } = null!;
    }
}
