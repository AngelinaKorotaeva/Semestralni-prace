using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkolniJidelna.Models;

namespace SkolniJidelna.Models
{
    [Table("SLOZKY")]
    public class Slozka
    {
        [Key]
        [Column("ID_SLOZKA")]
        public int IdSlozka { get; set; }

        [Column("NAZEV")]
        public string Nazev { get; set; } = null!;

        [Column("MERNA_JEDNOTKA")]
        public string MernaJednotka { get; set; } = null!;

        [Column("DATUM_PLATNOSTI")]
        public DateTime DatumPlatnosti { get; set; }

        public ICollection<SlozkaJidlo>? SlozkyJidla { get; set; }
    }
}
