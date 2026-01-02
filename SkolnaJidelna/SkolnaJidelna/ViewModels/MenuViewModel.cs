using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;

namespace SkolniJidelna.ViewModels
{
    // ViewModel pro zobrazení seznamu menu a jejich jídel
    public class MenuViewModel : BaseViewModel
    {
        private ObservableCollection<MenuNode> _menus = new();
        private string _selectedTypMenu = "Vše";

        // Kolekce položek menu pro binding do UI
        public ObservableCollection<MenuNode> Menus { get => _menus; private set { _menus = value; RaisePropertyChanged(); } }

        // Položky pro ComboBox typů menu
        public ObservableCollection<TypMenuItem> TypyMenuItems { get; } = new()
        {
            new TypMenuItem { Key = "Vše", Nazev = "Vše" },
            new TypMenuItem { Key = "SNIDANE", Nazev = "Snidaně" },
            new TypMenuItem { Key = "OBED", Nazev = "Oběd" }
        };

        public string SelectedTypMenu
        {
            get => _selectedTypMenu;
            set { _selectedTypMenu = value; RaisePropertyChanged(); }
        }

        public ICommand FilterCommand { get; }

        public MenuViewModel()
        {
            FilterCommand = new RelayCommand(_ => LoadMenus());
        }

        public class TypMenuItem
        {
            public string Key { get; set; } = string.Empty; // hodnoty: "Vše", "SNIDANE", "OBED"
            public string Nazev { get; set; } = string.Empty; // zobrazení: "Vše", "Snidaně", "Oběd"
        }

        // Načte menu s aktuálním filtrem SelectedTypMenu včetně jejich jídel a stavu menu
        public void LoadMenus()
        {
            using var ctx = new AppDbContext();

            var query = ctx.Menu.AsNoTracking();
            var selected = (SelectedTypMenu ?? "").Trim();
            if (!string.Equals(selected, "Vše", System.StringComparison.OrdinalIgnoreCase))
            {
                var dbTyp = selected.ToUpperInvariant(); // SNIDANE nebo OBED
                query = query.Where(m => m.TypMenu != null && m.TypMenu.ToUpper() == dbTyp);
            }

            var rawMenus = query
                .OrderBy(m => m.IdMenu)
                .Select(m => new { m.IdMenu, m.Nazev, m.TypMenu, m.TimeOd, m.TimeDo })
                .ToList();

            var list = new ObservableCollection<MenuNode>();

            // Přímé volání funkce F_STAV_MENU pro zjištění aktuálního stavu
            var conn = ctx.Database.GetDbConnection();
            var needClose = conn.State != System.Data.ConnectionState.Open;
            if (needClose) conn.Open();
            try
            {
                foreach (var rm in rawMenus)
                {
                    var node = new MenuNode
                    {
                        IdMenu = rm.IdMenu,
                        Nazev = rm.Nazev,
                        TypMenu = rm.TypMenu ?? string.Empty,
                        TimeOd = rm.TimeOd,
                        TimeDo = rm.TimeDo,
                    };

                    // Stav menu
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = "SELECT F_STAV_MENU(:p_id) FROM dual";
                        var p = cmd.CreateParameter();
                        p.ParameterName = ":p_id";
                        p.Value = node.IdMenu;
                        p.DbType = System.Data.DbType.Int32;
                        cmd.Parameters.Add(p);
                        var res = cmd.ExecuteScalar();
                        node.StavText = res == null || res == DBNull.Value ? string.Empty : res.ToString() ?? string.Empty;
                    }

                    // IDs jídel pro dané menu
                    var jidlaIds = ctx.Jidlo.AsNoTracking()
                        .Where(j => j.IdMenu == node.IdMenu)
                        .Select(j => j.IdJidlo)
                        .ToList();

                    // Detaily jídel z view V_JIDLA_SLOZENI
                    var foods = ctx.VJidlaSlozeni.AsNoTracking()
                        .Where(v => jidlaIds.Contains(v.IdJidlo))
                        .OrderBy(v => v.NazevJidla)
                        .Select(v => new JidloItem
                        {
                            IdJidlo = v.IdJidlo,
                            Nazev = v.NazevJidla,
                            Popis = v.PopisJidla,
                            Cena = v.Cena,
                            Poznamka = v.Slozeni // reuse Poznamka field to display composition text
                        })
                        .ToList();

                    foreach (var ji in foods)
                        node.Jidla.Add(ji);

                    list.Add(node);
                }
            }
            finally
            {
                if (needClose) conn.Close();
            }

            Menus = list;
        }
    }

    // Uzly pro TreeView (root = Menu, child = Jidlo)
    public class MenuNode : BaseViewModel
    {
        public int IdMenu { get; set; }
        public string Nazev { get; set; } = string.Empty;
        public string TypMenu { get; set; } = string.Empty;
        public DateTime? TimeOd { get; set; }
        public DateTime? TimeDo { get; set; }
        public string StavText { get; set; } = string.Empty;
        public ObservableCollection<JidloItem> Jidla { get; } = new();
    }

    public class JidloItem : BaseViewModel
    {
        public int IdJidlo { get; set; }
        public string Nazev { get; set; } = string.Empty;
        public string? Popis { get; set; }
        public double Cena { get; set; }
        public string? Poznamka { get; set; }
    }
}

