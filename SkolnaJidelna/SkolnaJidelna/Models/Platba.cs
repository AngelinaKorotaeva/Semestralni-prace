using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    public class Platba
    {
        public int IdPlatba { get; set; }
        public DateTime Datum { get; set; }
        public double Castka { get; set; }
        public string Metoda { get; set; } = null!;

        public int IdStravnik { get; set; }
        public Stravnik Stravnik { get; set; } = null!;
    }
}
