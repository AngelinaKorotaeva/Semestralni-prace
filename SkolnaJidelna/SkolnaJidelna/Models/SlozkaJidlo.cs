using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SkolniJidelna.Models
{
    [Table("SLOZKY_JIDLA")]
    public class SlozkaJidlo
    {
        [Column("ID_JIDLO")]
        public int IdJidlo { get; set; }

        [Column("ID_SLOZKA")]
        public int IdSlozka { get; set; }

        [Column("MNOZSTVI")]
        public int Mnozstvi { get; set; }

        [Column("POZNAMKA")]
        public string? Poznamka { get; set; }

        [ForeignKey(nameof(IdJidlo))]
        public Jidlo Jidlo { get; set; } = null!;

        [ForeignKey(nameof(IdSlozka))]
        public Slozka Slozka { get; set; } = null!;
    }
}
