using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkolniJidelna.Models
{
    [Table("V_STUD_TRIDA")]
    public class VStudTrida
    {
        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [Column("JMENO")]
        public string Jmeno { get; set; }

        [Column("DATUM_NAROZENI")]
        public DateTime DatumNarozeni { get; set; }

        [Column("EMAIL")]
        public string Email { get; set; }

        [Column("CISLO_TRIDY")]
        public int CisloTridy { get; set; }

        [Column("ALERGIE")]
        public string? Alergie { get; set; }

        [Column("OMEZENI")]
        public string? Omezeni { get; set; }
    }
}