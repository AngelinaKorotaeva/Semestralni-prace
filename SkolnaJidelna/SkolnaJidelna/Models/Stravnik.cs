using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SkolniJidelna.Models;

namespace SkolniJidelna.Models
{
    [Table("STRAVNICI")]
    public class Stravnik
    {
        [Key]
        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }
        
        [Column("JMENO")]
        public string Jmeno { get; set; } = null!;
        
        [Column("PRIJMENI")]
        public string Prijmeni { get; set; } = null!;
        
        [Column("EMAIL")]
        public string Email { get; set; } = null!;
        
        [Column("HESLO")]
        public string? Heslo { get; set; }

        [Column("ZUSTATEK")]
        public double Zustatek { get; set; }

        [Column("ROLE")]
        public string? Role { get; set; }

        [Column("AKTIVITA")]
        public char? Aktivita { get; set; }

        [Column("TYP_STRAVNIK")]
        public string? TypStravnik { get; set; }

        [Column("ID_ADRESA")]
        public int? IdAdresa { get; set; }

        [ForeignKey(nameof(IdAdresa))]
        public Adresa? Adresa { get; set; }

        public ICollection<Objednavka>? Objednavky { get; set; }
        public ICollection<Platba>? Platby { get; set; }
        public ICollection<Soubor>? Soubory { get; set; }
        public ICollection<StravnikAlergie>? Alergie { get; set; }
        public ICollection<StravnikOmezeni>? Omezeni { get; set; }
    }
}
