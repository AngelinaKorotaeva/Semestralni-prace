using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class StravnikAlergie
    {
        public int IdStravnik { get; set; }
        public int IdAlergie { get; set; }

        public Stravnik Stravnik { get; set; } = null!;
        public Alergie Alergie { get; set; } = null!;
    }
}
