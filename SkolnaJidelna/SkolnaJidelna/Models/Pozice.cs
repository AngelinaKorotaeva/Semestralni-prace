using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Pozice
    {
        private int IdPozice {  get; set; }
        public string Nazev {  get; set; }
        public ICollection<Pracovnik> Pracovnici { get; set; }
    }
}
