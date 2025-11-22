using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    [Table("POZICE")]
    public class Pozice
    {
        [Key]
        [Column("ID_POZICE")]
        public int IdPozice { get; set; }

        [Column("NAZEV")]
        public string Nazev { get; set; } = null!;

        public ICollection<Pracovnik>? Pracovnici { get; set; }
    }
}
