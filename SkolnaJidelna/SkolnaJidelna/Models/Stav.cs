using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    [Table("STAVY")]
    public class Stav
    {
        [Key]
        [Column("ID_STAV")]
        public int IdStav { get; set; }

        [Column("NAZEV")]
        public string Nazev { get; set; } = null!;
    }
}
