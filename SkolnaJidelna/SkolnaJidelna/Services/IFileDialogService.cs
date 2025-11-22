using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Services
{
    public interface IFileDialogService
    {
        // Vrací absolutní cestu k vybranému souboru nebo null pokud uživatel zrušil.
        string? OpenFileDialog(string filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*");
    }
}