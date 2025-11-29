using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SkolniJidelna.Models
{
    [Keyless]
    [Table("V_STRAVNICI_LOGIN")]
    public class VStravnikLogin
    {
        [Column("ID_STRAVNIK")]
        public int IdStravnik { get; set; }

        [Column("EMAIL")]
        public string Email { get; set; } = null!;

        [Column("HESLO")]
        public string? Heslo { get; set; }

        [Column("ROLE")]
        public string? Role { get; set; }

        [Column("TYP_STRAVNIK")]
        public string TypStravnik { get; set; } = null!;

        [Column("AKTIVITA")]
        public string Aktivita { get; set; } = null!;
    }
}
