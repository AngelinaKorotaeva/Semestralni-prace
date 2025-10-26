using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Slozka
    {
        private int IdSlozka {  get; set; }
        public string Nazev {  get; set; }
        public string MernaJednotka { get; set; }
        public DateTime DatumPlatnosti { get; set; }

        public ICollection<SlozkaJidlo> SlozkaJidlos { get; set; }
    }
}
