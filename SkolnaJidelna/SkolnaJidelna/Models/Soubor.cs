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
    [Table("SOUBORY")]
    public class Soubor
    {
        [Key]
        [Column("ID_SOUBOR")]
        public int IdSoubor { get; set; }

        [Column("NAZEV")]
        public string Nazev { get; set; } = null!;

        [Column("TYP")]
        public string Typ { get; set; } = null!;

        [Column("PRIPONA")]
        public string Pripona { get; set; } = null!;

        [Column("OBSAH")]
        public byte[] Obsah { get; set; } = null!;

        [Column("DATUM_NAHRANI")]
        public DateTime DatumNahrani { get; set; }

        [Column("DATUM_MODIFIKACE")]
        public DateTime? DatumModifikace { get; set; }
        public string? Operace { get; set; }

        [Column("TABULKA")]
        public string Tabulka { get; set; } = null!;

        [Column("ID_ZAZNAM")]
        public int IdZaznam { get; set; }

        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [ForeignKey(nameof(IdStravnik))]
        public Stravnik Stravnik { get; set; } = null!;
    }
}
