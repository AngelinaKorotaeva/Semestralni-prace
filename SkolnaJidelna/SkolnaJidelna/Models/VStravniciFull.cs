using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SkolniJidelna.Models
{
    // Unified view for users with address, worker/student extras
    [Table("V_STRAVNICI_FULL")] // actual mapping via ToView in DbContext
    public class VStravniciFull
    {
        [Column("ID_STRAVNIK")] public int IdStravnik { get; set; }
        [Column("JMENO")] public string Jmeno { get; set; } = string.Empty;
        [Column("PRIJMENI")] public string Prijmeni { get; set; } = string.Empty;
        [Column("EMAIL")] public string Email { get; set; } = string.Empty;
        [Column("ROLE")] public string? Role { get; set; }
        [Column("TYP_STRAVNIK")] public string? TypStravnik { get; set; }
        [Column("ZUSTATEK")] public double Zustatek { get; set; }
        [Column("PSC")] public int? Psc { get; set; }
        [Column("ULICE")] public string? Ulice { get; set; }
        [Column("MESTO")] public string? Mesto { get; set; }
        [Column("TELEFON")] public int? Telefon { get; set; }
        [Column("POZICE")] public string? Pozice { get; set; }
        [Column("ID_TRIDA")] public int? IdTrida { get; set; }
        [Column("DATUM_NAROZENI")] public DateTime? DatumNarozeni { get; set; }
    }
}