using System;

namespace SkolniJidelna.Services
{
    /// <summary>
    /// Služba dialogu souborů – abstrakce nad OS/UI pro MVVM a testování.
    /// Umožňuje ViewModelům vybírat soubory bez přímé závislosti na WPF API.
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>
        /// Otevře dialog pro výběr souboru a vrátí absolutní cestu, nebo `null` při zrušení.
        /// Parametr `filter` omezuje typy souborů (ve formátu OpenFileDialog).
        /// </summary>
        string? OpenFileDialog(string filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*");
    }
}