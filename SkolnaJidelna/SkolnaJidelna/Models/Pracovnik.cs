using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Pracovnik
    {
        public int Telefon {  get; set; }
        public int IdPozice { get; set; }
        public Pozice Pozice { get; set; }
    }
}
