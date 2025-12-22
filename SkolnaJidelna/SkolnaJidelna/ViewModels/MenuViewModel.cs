using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using SkolniJidelna.Data;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels
{
    public class MenuViewModel : BaseViewModel
    {
        private ObservableCollection<JidloViewModel> _jidla = new();
        private string _selectedTypMenu = "Vše";
        private string _selectedTyden = "Vše";

        // Grouped menus -> jidla for TreeView binding
        private ObservableCollection<MenuWithJidla> _menus = new();

        // Old simple list (not used by ComboBox now but kept if needed elsewhere)
        public ObservableCollection<string> TypyMenu { get; } = new(new[] { "Vše", "SNIDANE", "OBED" });

        // New list to support DisplayMemberPath/SelectedValuePath like in LoginWindow
        public class TypMenuItem
        {
            public string Key { get; set; } = string.Empty; // selected value
            public string Nazev { get; set; } = string.Empty; // displayed text
        }

        public ObservableCollection<TypMenuItem> TypyMenuItems { get; } = new();

        public ObservableCollection<JidloViewModel> Jidla
        {
            get => _jidla;
            set { _jidla = value; RaisePropertyChanged(); }
        }

        public ObservableCollection<MenuWithJidla> Menus
        {
            get => _menus;
            set { _menus = value; RaisePropertyChanged(); }
        }

        public string SelectedTypMenu
        {
            get => _selectedTypMenu;
            set { _selectedTypMenu = value; RaisePropertyChanged(); }
        }

        public string SelectedTyden
        {
            get => _selectedTyden;
            set { _selectedTyden = value; RaisePropertyChanged(); }
        }

        public ICommand FilterCommand { get; }

        public MenuViewModel()
        {
            // Populate items for ComboBox like in LoginWindow (DisplayMemberPath/SelectedValuePath)
            TypyMenuItems.Add(new TypMenuItem { Key = "Vše", Nazev = "Vše" });
            TypyMenuItems.Add(new TypMenuItem { Key = "SNIDANE", Nazev = "SNIDANE" });
            TypyMenuItems.Add(new TypMenuItem { Key = "OBED", Nazev = "OBED" });

            // ensure default is present and selected
            if (!TypyMenuItems.Any(i => string.Equals(i.Key, _selectedTypMenu, StringComparison.OrdinalIgnoreCase)))
                _selectedTypMenu = TypyMenuItems.First().Key;

            FilterCommand = new RelayCommand(_ => { LoadMenus(); });
            LoadAllJidla();
            // Menus will be loaded only when the user clicks Filtrovat
            Menus = new ObservableCollection<MenuWithJidla>();
        }

        private void LoadAllJidla()
        {
            try
            {
                using var ctx = new AppDbContext();
                var allJidla = ctx.Jidlo
                    .Include(j => j.Menu)
                    .Include(j => j.SlozkyJidla)
                        .ThenInclude(sj => sj.Slozka)
                    .ToList();
                Jidla = new ObservableCollection<JidloViewModel>(allJidla.Select(j => new JidloViewModel(j)));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri nacitani jidel: " + ex.Message);
            }
        }

        private void FilterJidla()
        {
            try
            {
                using var ctx = new AppDbContext();
                var query = ctx.Jidlo
                    .Include(j => j.Menu)
                    .Include(j => j.SlozkyJidla)
                        .ThenInclude(sj => sj.Slozka)
                    .AsQueryable();

                if (!string.Equals(SelectedTypMenu ?? "Vše", "Vše", StringComparison.OrdinalIgnoreCase))
                {
                    var typ = (SelectedTypMenu ?? string.Empty).Trim().ToUpperInvariant();
                    query = query.Where(j => j.Menu != null && j.Menu.TypMenu != null && j.Menu.TypMenu.ToUpper() == typ);
                }

                // TODO: Filter by week if Menu has tэden information

                var filtered = query.ToList().Select(j => new JidloViewModel(j));
                Jidla = new ObservableCollection<JidloViewModel>(filtered);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri filtrovani: " + ex.Message);
            }
        }

        // Data models for TreeView grouping (ViewModel-friendly)
        public class JidloItem
        {
            public string Nazev { get; set; } = string.Empty;
            public string? Popis { get; set; }
            public double Cena { get; set; }
            public string? Poznamka { get; set; }
        }

        public class MenuWithJidla
        {
            public int IdMenu { get; set; }
            public string Nazev { get; set; } = string.Empty;
            public string TypMenu { get; set; } = string.Empty;
            public ObservableCollection<JidloItem> Jidla { get; set; } = new();
        }

        public void LoadMenus()
        {
            try
            {
                using var ctx = new AppDbContext();
                var menusQuery = ctx.Menu.AsNoTracking();
                var selected = (SelectedTypMenu ?? "").Trim();
                if (!string.Equals(selected, "Vše", StringComparison.OrdinalIgnoreCase))
                {
                    var typ = selected.ToUpperInvariant();
                    menusQuery = menusQuery.Where(m => m.TypMenu != null && m.TypMenu.ToUpper() == typ);
                }

                var list = menusQuery
                    .OrderBy(m => m.IdMenu)
                    .Select(m => new MenuWithJidla
                    {
                        IdMenu = m.IdMenu,
                        Nazev = m.Nazev,
                        TypMenu = m.TypMenu ?? string.Empty,
                        Jidla = new ObservableCollection<JidloItem>(
                            ctx.Jidlo
                                .AsNoTracking()
                                .Where(j => j.IdMenu == m.IdMenu)
                                .OrderBy(j => j.Nazev)
                                .Select(j => new JidloItem
                                {
                                    Nazev = j.Nazev,
                                    Popis = j.Popis,
                                    Cena = j.Cena,
                                    Poznamka = j.Poznamka
                                })
                                .ToList())
                    })
                    .ToList();

                Menus = new ObservableCollection<MenuWithJidla>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri nacitani menu: " + ex.Message);
                Menus = new ObservableCollection<MenuWithJidla>();
            }
        }
    }
}

