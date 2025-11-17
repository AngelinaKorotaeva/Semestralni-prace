using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    public class SlozkaJidlo
    {
        public int IdJidlo { get; set; }
        public int IdSlozka { get; set; }
        public int Mnozstvi { get; set; }
        public string? Poznamka { get; set; }

        public Jidlo Jidlo { get; set; } = null!;
        public Slozka Slozka { get; set; } = null!;
    }
}
