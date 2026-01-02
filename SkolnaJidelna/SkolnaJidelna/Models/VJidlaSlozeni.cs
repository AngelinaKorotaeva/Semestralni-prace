using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkolniJidelna.Models
{
    [Table("V_JIDLA_SLOZENI")]
    public class VJidlaSlozeni
    {
        [Column("ID_JIDLO")]
        public int IdJidlo { get; set; }

        [Column("NAZEV_JIDLA")]
        public string NazevJidla { get; set; }

        [Column("POPIS_JIDLA")]
        public string PopisJidla { get; set; }

        [Column("KATEGORIE")]
        public string Kategorie { get; set; }

        [Column("CENA")]
        public double Cena { get; set; }

        [Column("SLOZENI")]
        public string Slozeni { get; set; }
    }
}