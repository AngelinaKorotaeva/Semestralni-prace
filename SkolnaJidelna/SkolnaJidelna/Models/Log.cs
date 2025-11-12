using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Log
    {
        public int IdLog { get; set; }
        public string Tabulka { get; set; } = null!;
        public int IdZaznam { get; set; }
        public string Akce { get; set; } = null!;
        public DateTime DatumCas { get; set; }
        public string? Detail { get; set; }
    }
}
