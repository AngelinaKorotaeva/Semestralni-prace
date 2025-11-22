using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.ViewModels
{
    public class ItemViewModel
    {
        public object Entity { get; }
        public string Summary { get; }

        public ItemViewModel(object entity, string summary)
        {
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
            Summary = summary ?? entity.GetType().Name;
        }
    }
}
