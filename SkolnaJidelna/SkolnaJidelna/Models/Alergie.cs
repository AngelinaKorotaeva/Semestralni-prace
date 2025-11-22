using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    [Table("ALERGIE")]
    public class Alergie
    {
        [Key]
        [Column("ID_ALERGIE")]
        public int IdAlergie { get; set; }

        [Column("NAZEV")]
        public string Nazev { get; set; } = null!;

        [Column("PRODUKT")]
        public string Produkt { get; set; } = null!;

        public ICollection<StravnikAlergie>? StravniciAlergie { get; set; }
    }
}
