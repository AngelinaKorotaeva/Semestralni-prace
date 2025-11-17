using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    public class Pracovnik
    {
        public int IdStravnik { get; set; }
        public int Telefon { get; set; }

        public int IdPozice { get; set; }
        public Pozice Pozice { get; set; } = null!;
        public Stravnik Stravnik { get; set; } = null!;
    }
}
