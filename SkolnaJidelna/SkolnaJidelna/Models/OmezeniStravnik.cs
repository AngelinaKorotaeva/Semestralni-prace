using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class OmezeniStravnik
    {
        public int IdStravnik { get; set; }
        public int IdOmezeni {  get; set; }

        public Stravnik Stravnik { get; set; }
        public DietniOmezeni DietniOmezeni { get; set; }
    }
}
