using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.ViewModels
{
    // Jednoduchý wrapper pro libovolnou entitu zobrazenou v seznamu v admin UI.
    // Uchovává samotnou entitu (Entity) a textový souhrn pro zobrazení (Summary).
    public class ItemViewModel
    {
        public object Entity { get; }
        public string Summary { get; }

        public ItemViewModel(object entity, string summary)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
            Summary = summary ?? entity.GetType().Name; // Fallback na název typu, pokud není dodán popis
        }
    }
}
