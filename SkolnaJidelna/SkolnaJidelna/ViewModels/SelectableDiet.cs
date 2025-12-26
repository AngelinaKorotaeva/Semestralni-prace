using System.ComponentModel;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels
{
    // Polo?ka pro v?b?r dietn?ho omezen? v UI (checkbox)
    // Udr?uje model `DietniOmezeni` a p??znak `IsSelected`
    public class SelectableDiet : INotifyPropertyChanged
    {
        private bool _isSelected;
        public DietniOmezeni Diet { get; set; }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}