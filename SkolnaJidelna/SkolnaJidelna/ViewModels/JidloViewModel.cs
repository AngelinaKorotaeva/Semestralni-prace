using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels
{
    // ViewModel pro prezentaci jedn? polo?ky Jidlo v UI
    // Poskytuje pouze ?tec? vlastnosti sv?zan? s modelem `Jidlo`
    public class JidloViewModel : INotifyPropertyChanged
    {
        private Jidlo _jidlo;

        public JidloViewModel(Jidlo jidlo)
        {
            _jidlo = jidlo;
        }

        public string Nazev => _jidlo.Nazev;                 // N?zev j?dla
        public string Popis => _jidlo.Popis;                 // Popis j?dla
        public string Kategorie => _jidlo.Kategorie;         // Kategorie (Pol?vka/Dezert/...)
        public double Cena => _jidlo.Cena;                   // Cena
        // Slo?en? jako text (seznam n?zv? slo?ek odd?len? ??rkami)
        public string SlozkyText => string.Join(", ", _jidlo.SlozkyJidla?.Select(sj => sj.Slozka?.Nazev).Where(n => !string.IsNullOrEmpty(n)) ?? new List<string>());

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}