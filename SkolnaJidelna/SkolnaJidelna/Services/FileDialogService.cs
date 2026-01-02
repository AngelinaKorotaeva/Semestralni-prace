using System;
using Microsoft.Win32;

namespace SkolniJidelna.Services
{
    /// <summary>
    /// Implementace služby `IFileDialogService` – otevírá systémový dialog pro výběr souboru.
    /// Odděluje UI logiku od ViewModelu a usnadňuje testování (možno nahradit mockem).
    /// </summary>
    public class FileDialogService : IFileDialogService
    {
        /// <summary>
        /// Otevře `OpenFileDialog` s daným filtrem a vrátí vybranou cestu, nebo `null` při zrušení.
        /// </summary>
        public string? OpenFileDialog(string filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*")
        {
            var dlg = new OpenFileDialog { Filter = filter };
            var result = dlg.ShowDialog();
            return result == true ? dlg.FileName : null;
        }
    }
}
