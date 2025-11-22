using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SkolniJidelna.Models
{
    [Table("STRAVNICI_OMEZENI")]
    public class StravnikOmezeni
    {
        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [Column("ID_OMEZENI")]
        public int IdOmezeni { get; set; }

        [ForeignKey(nameof(IdStravnik))]
        public Stravnik Stravnik { get; set; } = null!;

        [ForeignKey(nameof(IdOmezeni))]
        public DietniOmezeni DietniOmezeni { get; set; } = null!;
    }
}
