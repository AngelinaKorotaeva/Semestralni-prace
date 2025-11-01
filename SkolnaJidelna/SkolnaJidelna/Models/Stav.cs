using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Stav
    {
        private int IdStav {  get; set; }
        public string Nazev { get; set; }
        public ICollection<Objednavka> Objednavky { get; set; }
    }
}
