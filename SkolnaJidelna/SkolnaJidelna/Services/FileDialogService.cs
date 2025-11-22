using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace SkolniJidelna.Services
{
    public class FileDialogService : IFileDialogService
    {
        public string? OpenFileDialog(string filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*")
        {
            var dlg = new OpenFileDialog
            {
                Filter = filter
            };

            var result = dlg.ShowDialog();
            if (result == true)
                return dlg.FileName;
            return null;
        }
    }
}
