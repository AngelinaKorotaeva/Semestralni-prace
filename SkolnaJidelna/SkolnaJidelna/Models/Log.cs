using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Log
    {
        private int IdLog {  get; set; }
        private string Tabulka { get; set; }
        private int IdZaznam { get; set; }
        private string Akce { get; set; }
        private DateOnly DatumCas {  get; set; }
        private string? Detail {  get; set; }
    }
}
