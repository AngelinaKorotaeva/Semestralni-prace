using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkolniJidelna.Models;

namespace SkolniJidelna.Models
{
    public class Slozka
    {
        public int IdSlozka { get; set; }
        public string Nazev { get; set; } = null!;
        public string MernaJednotka { get; set; } = null!;
        public DateTime DatumPlatnosti { get; set; }

        public ICollection<SlozkaJidlo>? SlozkyJidla { get; set; }
    }
}
