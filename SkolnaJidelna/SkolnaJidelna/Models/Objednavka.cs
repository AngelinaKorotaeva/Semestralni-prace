using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SkolniJidelna.Models
{
    [Table("OBJEDNAVKY")]
    public class Objednavka
    {
        [Key]
        [Column("ID_OBJEDNAVKA")]
        public int IdObjednavka { get; set; }

        [Column("DATUM")]
        public DateTime Datum { get; set; }

        [Column("CELKOVA_CENA")]
        public double CelkovaCena { get; set; }

        [Column("POZNAMKA")]
        public string? Poznamka { get; set; }

        [Column("ID_STAV")]
        public int IdStav { get; set; }
        public Stav Stav { get; set; } = null!;


        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [ForeignKey(nameof(IdStravnik))]
        public Stravnik Stravnik { get; set; } = null!;

        public ICollection<Polozka>? Polozky { get; set; }
    }
}
