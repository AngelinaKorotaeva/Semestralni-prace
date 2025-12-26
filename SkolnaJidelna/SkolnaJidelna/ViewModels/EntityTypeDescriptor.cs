using System;

namespace SkolniJidelna.ViewModels
{
    // Popis typu entity pro admin UI: zobrazované jméno, CLR typ a asynchronní loader položek
    public class EntityTypeDescriptor
    {
        public string Name { get; init; } = null!;       // Zobrazený název (např. "Studenti")
        public Type EntityType { get; init; } = null!;    // CLR typ entity (např. typeof(Student))
        public Func<System.Threading.Tasks.Task<System.Collections.Generic.List<ItemViewModel>>>? LoaderAsync { get; init; } // Delegate pro načtení položek
    }
}
