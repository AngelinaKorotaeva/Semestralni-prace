using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    [Table("LOGY")]
    public class Log
    {
        [Key]
        [Column("ID_LOG")]
        public int IdLog { get; set; }

        [Column("TABULKA")]
        public string Tabulka { get; set; } = null!;

        [Column("ID_ZAZNAM")]
        public int IdZaznam { get; set; }

        [Column("AKCE")]
        public string Akce { get; set; } = null!;

        [Column("DATUM_CAS")]
        public DateTime DatumCas { get; set; }

        [Column("DETAIL")]
        public string? Detail { get; set; }
    }
}
