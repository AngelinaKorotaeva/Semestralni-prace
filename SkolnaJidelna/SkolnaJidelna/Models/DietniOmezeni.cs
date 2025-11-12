using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class DietniOmezeni
    {
        public int IdOmezeni { get; set; }
        public string Nazev { get; set; } = null!;
        public string Popis { get; set; } = null!;

        public ICollection<StravnikOmezeni>? StravniciOmezeni { get; set; }
    }
}
