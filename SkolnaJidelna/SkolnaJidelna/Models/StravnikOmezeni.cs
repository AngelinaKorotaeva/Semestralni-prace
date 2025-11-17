using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    public class StravnikOmezeni
    {
        public int IdStravnik { get; set; }
        public int IdOmezeni { get; set; }

        public Stravnik Stravnik { get; set; } = null!;
        public DietniOmezeni DietniOmezeni { get; set; } = null!;
    }
}
