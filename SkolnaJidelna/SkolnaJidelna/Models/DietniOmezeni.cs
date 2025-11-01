using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class DietniOmezeni
    {
        private int IdOmezeni {  get; set; }
        public string Nazev {  get; set; }
        public string Popsani {  get; set; }

        public ICollection<OmezeniStravnik> OmezeniStravniky { get; set; }
    }
}
