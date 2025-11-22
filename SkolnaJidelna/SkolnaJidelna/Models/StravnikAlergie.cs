using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SkolniJidelna.Models
{
    [Table("STRAVNICI_ALERGIE")]
    public class StravnikAlergie
    {
        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [Column("ID_ALERGIE")]
        public int IdAlergie { get; set; }

        [ForeignKey(nameof(IdStravnik))]
        public Stravnik Stravnik { get; set; } = null!;

        [ForeignKey(nameof(IdAlergie))]
        public Alergie Alergie { get; set; } = null!;
    }
}
