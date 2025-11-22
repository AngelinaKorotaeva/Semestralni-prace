using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SkolniJidelna.Models
{
    [Table("PLATBY")]
    public class Platba
    {
        [Key]
        [Column("ID_PLATBA")]
        public int IdPlatba { get; set; }

        [Column("DATUM")]
        public DateTime Datum { get; set; }

        [Column("CASTKA")]
        public double Castka { get; set; }

        [Column("METODA")]
        public string Metoda { get; set; } = null!;

        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [ForeignKey(nameof(IdStravnik))]
        public Stravnik Stravnik { get; set; } = null!;
    }
}
