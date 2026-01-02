using System.ComponentModel.DataAnnotations.Schema;

namespace SkolniJidelna.Models
{
    // Summary view for AdminPanel: order header + email + items count + aggregated items text
    [Table("V_OBJ_PREHLED_ADMIN")]
    public class VObjPrehledAdmin
    {
        [Column("ID_OBJEDNAVKA")] public int IdObjednavka { get; set; }
        [Column("DATUM")] public System.DateTime Datum { get; set; }
        [Column("STAV_OBJEDNAVKY")] public string StavObjednavky { get; set; } = string.Empty;
        [Column("CELKOVA_CENA")] public double CelkovaCena { get; set; }
        [Column("EMAIL")] public string Email { get; set; } = string.Empty;
        [Column("POCET_POLOZEK")] public int Polozek { get; set; }
        [Column("JIDLA_V_OBJEDNAVCE")] public string? Jidla { get; set; }
    }
}