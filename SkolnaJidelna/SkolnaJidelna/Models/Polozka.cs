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
    [Table("POLOZKY")]
    public class Polozka
    {
        [Column("ID_JIDLO")]
        public int IdJidlo { get; set; }

        [Column("ID_OBJEDNAVKA")]
        public int IdObjednavka { get; set; }

        [Column("MNOZSTVI")]
        public int Mnozstvi { get; set; }

        [Column("CENA_POLOZKY")]
        public double CenaPolozky { get; set; }

        [ForeignKey(nameof(IdJidlo))]
        public Jidlo Jidlo { get; set; } = null!;

        [ForeignKey(nameof(IdObjednavka))]
        public Objednavka Objednavka { get; set; } = null!;
    }
}
