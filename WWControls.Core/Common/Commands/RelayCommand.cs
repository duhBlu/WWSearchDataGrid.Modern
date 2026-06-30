using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace WWControls.Core
{
    /// <summary>
    /// <c>CommandManager</c> below is Core's OWN requery hub (this assembly has no WPF
    /// reference), so WPF's input-driven auto-requery NEVER reaches these commands.
    /// CanExecute re-evaluation only happens when raised explicitly — per instance via
    /// <see cref="RaiseCanExecuteChanged"/> (also nudges the library-wide hub, since some
    /// call sites recreate command instances per access), or library-wide via
    /// <see cref="CommandManager.InvalidateRequerySuggested"/>.
    /// </summary>
    internal class RelayCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Predicate<object> canExecute;
        private EventHandler _canExecuteChanged;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { _canExecuteChanged += value; CommandManager.RequerySuggested += value; }
            remove { _canExecuteChanged -= value; CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
            CommandManager.InvalidateRequerySuggested();
        }
    }

    /// <inheritdoc cref="RelayCommand" path="/summary"/>
    internal class RelayCommand<T> : ICommand
    {
        private readonly Action<T> execute;
        private readonly Predicate<T> canExecute;
        private EventHandler _canExecuteChanged;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { _canExecuteChanged += value; CommandManager.RequerySuggested += value; }
            remove { _canExecuteChanged -= value; CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            if (parameter is T t) return canExecute?.Invoke(t) ?? true;
            return false;
        }


        public void Execute(object parameter)
        {
            if (parameter is T t)
                execute(t);
        }

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
