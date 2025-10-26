using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Objednavka
    {
        private int IdObjednavka {  get; set; }
        public DateTime Datum { get; set; }
        public int CelkovaCena { get; set; }
        public string? Poznamka { get; set; }

        public ICollection<Polozka> Polozky { get; set; }
    }
}
