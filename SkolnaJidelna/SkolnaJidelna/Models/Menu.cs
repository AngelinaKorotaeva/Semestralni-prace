using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    [Table("MENU")]
    public class Menu
    {
        [Key]
        [Column("ID_MENU")]
        public int IdMenu { get; set; }

        [Column("NAZEV")]
        public string Nazev { get; set; } = null!;

        [Column("TYP_MENU")]
        public string TypMenu { get; set; } = null!;

        [Column("TIME_OD")]
        public DateTime TimeOd { get; set; }

        [Column("TIME_DO")]
        public DateTime TimeDo { get; set; }

        public ICollection<Jidlo>? Jidla { get; set; }
    }
}
