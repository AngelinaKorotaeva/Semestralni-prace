using System;

namespace SkolniJidelna.Services
{
    // Rozhraní služby pro práci s File Open dialogem – umožňuje DI a testování bez UI
    public interface IFileDialogService
    {
        // Vrací absolutní cestu k vybranému souboru nebo null pokud uživatel zrušil.
        string? OpenFileDialog(string filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*");
    }
}