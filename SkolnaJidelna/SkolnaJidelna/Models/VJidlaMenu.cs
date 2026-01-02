using System.ComponentModel.DataAnnotations.Schema;

namespace SkolniJidelna.Models
{
    // EF model mapped to database view V_JIDLA_MENU
    [Table("V_JIDLA_MENU")] // hint for clarity; actual mapping via ToView in DbContext
    public class VJidlaMenu
    {
        [Column("ID_JIDLO")] public int IdJidlo { get; set; }
        [Column("JIDLO")] public string Jidlo { get; set; } = string.Empty;
        [Column("KATEGORIE")] public string Kategorie { get; set; } = string.Empty;
        [Column("POPIS")] public string Popis { get; set; } = string.Empty;
        [Column("CENA")] public double Cena { get; set; }
        [Column("MENU_NAZEV")] public string MenuNazev { get; set; } = string.Empty;
    }
}