using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Platba
    {
        private int IdPlatba {  get; set; }
        public DateTime Datum { get; set; }
        public int Castka { get; set; }
        public string Metoda { get; set; }
    }
}
