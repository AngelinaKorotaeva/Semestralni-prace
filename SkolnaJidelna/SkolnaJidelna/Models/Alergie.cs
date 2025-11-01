using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Alergie
    {
        private int IdAlergie {  get; set; }
        public string Nazev {  get; set; }
        public string product { get; set; }

        public ICollection<AlergieStravnik> AlergieStravniky { get; set; }
    }
}
