using System.ComponentModel;
using SkolniJidelna.Models;

namespace SkolniJidelna.ViewModels
{
    public class SelectableAlergie : INotifyPropertyChanged
    {
        private bool _isSelected;
        public Alergie Alergie { get; set; }
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