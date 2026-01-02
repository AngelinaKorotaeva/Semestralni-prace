using System.ComponentModel.DataAnnotations.Schema;

namespace SkolniJidelna.Models
{
    // Aggregated view of stravnici with allergies and diet restrictions
    [Table("V_STR_OMEZENI_ALERGIE")] // map to DB view name
    public class VStravnikOmezeniAlergie
    {
        [Column("ID_STRAVNIK")] public int IdStravnik { get; set; }
        [Column("JMENO")] public string Jmeno { get; set; } = string.Empty;
        [Column("PRIJMENI")] public string Prijmeni { get; set; } = string.Empty;
        [Column("EMAIL")] public string Email { get; set; } = string.Empty;
        [Column("ALERGIE")] public string? Alergie { get; set; }
        [Column("DIETNI_OMEZENI")] public string? DietniOmezeni { get; set; }
    }
}