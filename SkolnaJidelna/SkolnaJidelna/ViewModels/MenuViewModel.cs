using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

        public ObservableCollection<JidloViewModel> Jidla
        {
            get => _jidla;
            set
            {
                _jidla = value;
                RaisePropertyChanged();
            }
        }

        public string SelectedTypMenu
        {
            get => _selectedTypMenu;
            set
            {
                _selectedTypMenu = value;
                RaisePropertyChanged();
                FilterJidla();
            }
        }

        public string SelectedTyden
        {
            get => _selectedTyden;
            set
            {
                _selectedTyden = value;
                RaisePropertyChanged();
                FilterJidla();
            }
        }

        public MenuViewModel()
        {
            LoadAllJidla();
        }

        private void LoadAllJidla()
        {
            try
            {
                using var ctx = new AppDbContext();
                var allJidla = ctx.Jidlo.Include("Menu").Include("SlozkyJidla.Slozka").ToList();
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
                var query = ctx.Jidlo.Include("Menu").Include("SlozkyJidla.Slozka").AsQueryable();

                if (SelectedTypMenu != "Vše")
                {
                    query = query.Where(j => j.Menu != null && j.Menu.TypMenu == SelectedTypMenu);
                }

                // ??? Tyden ????? ???????? ??????, ???? ???? ???? ? Menu

                var filtered = query.ToList().Select(j => new JidloViewModel(j));
                Jidla = new ObservableCollection<JidloViewModel>(filtered);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Chyba pri filtrovani: " + ex.Message);
            }
        }
    }
}