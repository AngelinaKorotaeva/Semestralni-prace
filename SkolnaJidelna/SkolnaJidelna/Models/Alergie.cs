//HEAD
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolnaJidelna.Models
{
    public class Alergie
    {
        public int IdAlergie { get; set; }
        public string Nazev { get; set; } = null!;
        public string Produkt { get; set; } = null!;

        public ICollection<StravnikAlergie>? StravniciAlergie { get; set; }
    }
}
