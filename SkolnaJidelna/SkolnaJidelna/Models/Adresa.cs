using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Adresa
    {
        private int IdAdresa {  get; set; }
        public int PSC {  get; set; }
        public string Obec {  get; set; }
        public string Ulice { get; set; }
        public ICollection<Stravnik>? Stravnici { get; set; }
    }
}
