using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkolniJidelna.Models;

namespace SkolniJidelna.Models
{
    [Table("TRIDY")]
    public class Trida
    {
        [Key]
        [Column("ID_TRIDA")]
        public int IdTrida { get; set; }

        [Column("CISLO_TRIDY")]
        public int CisloTridy { get; set; }

        public ICollection<Student>? Studenti { get; set; }
    }
}
