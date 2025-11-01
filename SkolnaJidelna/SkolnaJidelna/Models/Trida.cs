using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Trida
    {
        private int IdTrida {  get; set; }
        public int CisloTridy { get; set; }
        public ICollection<Student>? Studenti { get; set; }
    }
}
