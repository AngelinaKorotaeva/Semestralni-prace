using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    public class Adresa
    {
        public int IdAdresa { get; set; }
        public int Psc { get; set; }
        public string Obec { get; set; } = null!;
        public string Ulice { get; set; } = null!;

        public ICollection<Stravnik>? Stravnici { get; set; }
    }
}
