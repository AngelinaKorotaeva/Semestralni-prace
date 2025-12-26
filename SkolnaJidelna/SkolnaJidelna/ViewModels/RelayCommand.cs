using System.Windows.Input;
using System;

namespace SkolniJidelna.ViewModels
{
    // Jednoduchá implementace ICommand pro MVVM – deleguje Execute/CanExecute na předané akce
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Vrací zda lze příkaz provést (přivolává se automaticky díky CommandManager)
        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        // Provede akci příkazu
        public void Execute(object? parameter) => _execute(parameter);

        // Připojení k CommandManager.RequerySuggested – WPF automaticky znovu dotazuje CanExecute
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value!;
            remove => CommandManager.RequerySuggested -= value!;
        }

        // Ruční vyvolání přepočtu CanExecute (např. po změně SelectedItem)
        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
    }
}