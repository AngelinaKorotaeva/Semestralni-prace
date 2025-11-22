using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.ViewModels
{
    public class EntityTypeDescriptor
    {
        public string Name { get; init; } = null!;
        public Type EntityType { get; init; } = null!;
        public Func<System.Threading.Tasks.Task<System.Collections.Generic.List<ItemViewModel>>>? LoaderAsync { get; init; }
    }
}
