using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Models
{
    [Keyless]
    [Table("V_OBJEDNAVKY_HISTORIE_DETAIL")]
    public class VObjHistorieDetail
    {
        [Column("ID_OBJEDNAVKA")]
        public int IdObjednavka { get; set; }

        [Column("DATUM_ODBERU")]
        public DateTime DatumOdberu { get; set; }

        [Column("DATUM_VYTVORENI")]
        public DateTime? DatumVytvoreni { get; set; }

        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [Column("JMENO")]
        public string Jmeno { get; set; }

        [Column("PRIJMENI")]
        public string Prijmeni { get; set; }

        [Column("STAV")]
        public string Stav { get; set; }

        [Column("ID_JIDLO")]
        public int IdJidlo { get; set; }

        [Column("JIDLO")]
        public string Jidlo { get; set; }

        [Column("MNOZSTVI")]
        public int Mnozstvi { get; set; }

        [Column("CENA_POLOZKY")]
        public double CenaPolozky { get; set; }

        [Column("CENA_POLOZKY_CELKEM")]
        public double CenaPolozkyCelkem { get; set; }

        [Column("CELKOVA_CENA")]
        public double CelkovaCena { get; set; }
    }
}
