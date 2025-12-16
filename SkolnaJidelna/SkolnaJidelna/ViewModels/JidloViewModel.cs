using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels
{
    public class JidloViewModel : INotifyPropertyChanged
    {
        private Jidlo _jidlo;

        public JidloViewModel(Jidlo jidlo)
        {
            _jidlo = jidlo;
        }

        public string Nazev => _jidlo.Nazev;
        public string Popis => _jidlo.Popis;
        public string Kategorie => _jidlo.Kategorie;
        public double Cena => _jidlo.Cena;
        public string SlozkyText => string.Join(", ", _jidlo.SlozkyJidla?.Select(sj => sj.Slozka?.Nazev).Where(n => !string.IsNullOrEmpty(n)) ?? new List<string>());

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}