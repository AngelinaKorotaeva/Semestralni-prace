using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Menu
    {
        private int IdMenu {  get; set; }
        public string Nazev {  get; set; }
        public string TypMenu { get; set; }
        public TimeOnly TimeOd { get; set; }
        public TimeOnly TimeDo { get; set; }
        public ICollection<Jidlo> Jidla { get; set; }
    }
}
