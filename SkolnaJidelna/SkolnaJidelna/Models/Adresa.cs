using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    [Table("ADRESY")]
    public class Adresa
    {
        [Key]
        [Column("ID_ADRESA")]
        public int IdAdresa { get; set; }

        [Column("PSC")]
        public int Psc { get; set; }

        [Column("MESTO")]
        public string Mesto { get; set; } = null!;

        [Column("ULICE")]
        public string Ulice { get; set; } = null!;

        public ICollection<Stravnik>? Stravnici { get; set; }
    }
}
