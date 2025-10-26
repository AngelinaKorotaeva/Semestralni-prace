using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Stravnik
    {
        private int IdStravnik {  get; set; }
        public string Jmeno { get; set; }
        public string Prijmeni { get; set; }
        public int Zustatek { get; set; }
        public string TypStravnik { get; set; }
        public string Email {  get; set; }

        public int IdAdresa { get; set; }
        public Adresa Adresa { get; set; }

        public ICollection<Objednavka>? Objednavky { get; set; }
        public ICollection<Platba>? Platby {  get; set; }
    }
}
