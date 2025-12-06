using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkolniJidelna.Services
{
    public interface IWindowService
    {
        void ShowAdminProfile(string adminEmail);
        void ShowUserProfile(string email, bool isPracovnik);
    }
}
