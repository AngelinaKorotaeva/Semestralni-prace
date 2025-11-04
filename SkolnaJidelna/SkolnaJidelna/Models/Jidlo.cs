using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Jidlo
    {
        private int IdJidlo { get; set; }
        public string Nazev {  get; set; }
        public string Popis {  get; set; }
        public string Kategorie { get; set; }
        public int Cena { get; set; }
        public string Poznamka { get; set; } = null;

        public int IdMenu {  get; set; }
        public Menu Menu { get; set; }

        public ICollection<Polozka> Polozky { get; set; }
        public ICollection<SlozkaJidlo> SlozkaJidlo { get; set; }
    }
}
