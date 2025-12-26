using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;

namespace SkolniJidelna.ViewModels
{
    // ViewModel pro zobrazení seznamu menu a jejich stavů
    public class MenuViewModel : BaseViewModel
    {
        private ObservableCollection<MenuItemVm> _menus = new();
        // Kolekce položek menu pro binding do UI
        public ObservableCollection<MenuItemVm> Menus { get => _menus; private set { _menus = value; RaisePropertyChanged(); } }

        // Položka menu včetně vypočteného stavu pomocí DB funkce F_STAV_MENU
        public class MenuItemVm : BaseViewModel
        {
            public int IdMenu { get; set; }
            public string Nazev { get; set; } = string.Empty;
            public string TypMenu { get; set; } = string.Empty;
            public DateTime? TimeOd { get; set; }
            public DateTime? TimeDo { get; set; }
            public string StavText { get; set; } = string.Empty; // Text stavu z F_STAV_MENU
        }

        // Načte všechna menu a pro každé zavolá F_STAV_MENU na databázi
        public void LoadMenus()
        {
            using var ctx = new AppDbContext();
            // Dotaz přes EF pro základní data menu
            var list = ctx.Menu.AsNoTracking()
                .OrderBy(m => m.IdMenu)
                .Select(m => new MenuItemVm
                {
                    IdMenu = m.IdMenu,
                    Nazev = m.Nazev,
                    TypMenu = m.TypMenu ?? string.Empty,
                    TimeOd = m.TimeOd,
                    TimeDo = m.TimeDo,
                })
                .ToList();

            // Přímé volání funkce F_STAV_MENU pro zjištění aktuálního stavu
            var conn = ctx.Database.GetDbConnection();
            var needClose = conn.State != ConnectionState.Open;
            if (needClose) conn.Open();
            try
            {
                foreach (var mi in list)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT F_STAV_MENU(:p_id) FROM dual";
                    var p = cmd.CreateParameter();
                    p.ParameterName = ":p_id";
                    p.Value = mi.IdMenu;
                    p.DbType = DbType.Int32;
                    cmd.Parameters.Add(p);
                    var res = cmd.ExecuteScalar();
                    mi.StavText = res == null || res == DBNull.Value ? string.Empty : res.ToString() ?? string.Empty;
                }
            }
            finally
            {
                if (needClose) conn.Close();
            }

            // Aktualizace kolekce pro UI
            Menus = new ObservableCollection<MenuItemVm>(list);
        }
    }
}

