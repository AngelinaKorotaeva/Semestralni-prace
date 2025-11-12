using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Student : Stravnik
    {
        //public int IdStravnik { get; set; }
        public DateTime DatumNarozeni { get; set; }

        public int IdTrida { get; set; }
        public Trida Trida { get; set; } = null!;
        public Stravnik Stravnik { get; set; } = null!;
    }
}
