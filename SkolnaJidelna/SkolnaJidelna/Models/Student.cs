using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using SkolniJidelna.Models;

namespace SkolniJidelna.Models
{
    [Table("STUDENTI")]
    public class Student
    {
        [Key]
        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [Column("DATUM_NAROZENI")]
        public DateTime DatumNarozeni { get; set; }

        [Column("ID_TRIDA")]
        public int IdTrida { get; set; }

        [ForeignKey(nameof(IdTrida))]
        public Trida Trida { get; set; } = null!;

        [ForeignKey(nameof(IdStravnik))]
        public Stravnik Stravnik { get; set; } = null!;
    }
}
