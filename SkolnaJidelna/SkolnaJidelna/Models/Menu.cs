using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Menu
    {
        public int IdMenu { get; set; }
        public string Nazev { get; set; } = null!;
        public string TypMenu { get; set; } = null!;
        public DateTime TimeOd { get; set; }
        public DateTime TimeDo { get; set; }

        public ICollection<Jidlo>? Jidla { get; set; }
    }
}
