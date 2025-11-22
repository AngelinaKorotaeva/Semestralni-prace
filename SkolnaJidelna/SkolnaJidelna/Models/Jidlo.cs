using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace SkolniJidelna.Models
{
    [Table("JIDLA")]
    public class Jidlo
    {
        [Key]
        [Column("ID_JIDLO")]
        public int IdJidlo { get; set; }

        [Column("NAZEV")]
        public string Nazev { get; set; } = null!;

        [Column("POPIS")]
        public string Popis { get; set; } = null!;

        [Column("KATEGORIE")]
        public string Kategorie { get; set; } = null!;

        [Column("CENA")]
        public double Cena { get; set; }

        [Column("POZNAMKA")]
        public string? Poznamka { get; set; }

        [Column("ID_MENU")]
        public int? IdMenu { get; set; }

        [ForeignKey(nameof(IdMenu))]
        public Menu? Menu { get; set; }

        public ICollection<Polozka>? Polozky { get; set; }
        public ICollection<SlozkaJidlo>? SlozkyJidla { get; set; }
    }
}
