using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkolniJidelna.Models
{
    [Table("V_PRACOVNIK_POZICE")]
    public class VPracovnikPozice
    {
        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [Column("JMENO")]
        public string Jmeno { get; set; }

        [Column("EMAIL")]
        public string Email { get; set; }

        [Column("POZICE")]
        public string Pozice { get; set; }

        [Column("TELEFON")]
        public string Telefon { get; set; }

        [Column("ALERGIE")]
        public string? Alergie { get; set; }

        [Column("OMEZENI")]
        public string? Omezeni { get; set; }
    }
}