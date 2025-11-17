using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkolniJidelna.Models;

namespace SkolniJidelna.Models
{
    public class Student
    {
        public int IdStravnik { get; set; }
        public DateTime DatumNarozeni { get; set; }

        public int IdTrida { get; set; }
        public Trida Trida { get; set; } = null!;
        public Stravnik Stravnik { get; set; } = null!;
    }
}
