using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class SlozkaJidlo
    {
        public int IdJidlo { get; set; }
        public int IdSlozka { get; set; }

        public int Mnozstvi {  get; set; }
        public string Poznamka { get; set; } = null;

        public Jidlo Jidlo { get; set; }
        public Slozka Slozka { get; set; }
    }
}
