using System;
using Microsoft.Win32;

namespace SkolniJidelna.Services
{
    // Služba pro otevření dialogu výběru souboru (abstrakce pro MVVM a testovatelnost)
    public class FileDialogService : IFileDialogService
    {
        // Otevře standardní OpenFileDialog s daným filtrem a vrátí vybranou cestu nebo null
        public string? OpenFileDialog(string filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*")
        {
            var dlg = new OpenFileDialog { Filter = filter };
            var result = dlg.ShowDialog();
            return result == true ? dlg.FileName : null;
        }
    }
}
