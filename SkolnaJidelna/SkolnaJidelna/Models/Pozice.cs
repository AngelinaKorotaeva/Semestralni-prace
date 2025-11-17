using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    public class Pozice
    {
        public int IdPozice { get; set; }
        public string Nazev { get; set; } = null!;

        public ICollection<Pracovnik>? Pracovnici { get; set; }
    }
}
