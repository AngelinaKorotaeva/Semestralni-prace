using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    [Table("DIETNI_OMEZENI")]
    public class DietniOmezeni
    {
        [Key]
        [Column("ID_OMEZENI")]
        public int IdOmezeni { get; set; }

        [Column("NAZEV")]
        public string Nazev { get; set; } = null!;

        [Column("POPIS")]
        public string Popis { get; set; } = null!;

        public ICollection<StravnikOmezeni>? StravniciOmezeni { get; set; }
    }
}
