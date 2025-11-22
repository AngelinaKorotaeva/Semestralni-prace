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
    [Table("PRACOVNICI")]
    public class Pracovnik
    {
        [Key]
        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [Column("TELEFON")]
        public int Telefon { get; set; }

        [Column("ID_POZICE")]
        public int IdPozice { get; set; }

        [ForeignKey(nameof(IdPozice))]
        public Pozice Pozice { get; set; } = null!;

        [ForeignKey(nameof(IdStravnik))]
        public Stravnik Stravnik { get; set; } = null!;
    }
}
