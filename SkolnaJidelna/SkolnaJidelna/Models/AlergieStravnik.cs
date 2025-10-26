using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class AlergieStravnik
    {
        public int IdStravnik { get; set; }
        public int IdAlergie {  get; set; }

        public Stravnik Stravnik { get; set; }
        public Alergie Alergie { get; set; }
    }
}
