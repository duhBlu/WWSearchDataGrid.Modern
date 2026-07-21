using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WWControls.Core
{
    /// <summary>
    /// An <see cref="ICommand"/> with an async, cancelable execute delegate. <see cref="IsExecuting"/>
    /// tracks whether a run is in flight; <see cref="Cancel"/> cancels the token passed to the delegate.
    /// Re-entry is blocked while running unless <c>allowConcurrentExecutions</c> is set.
    /// <see cref="OperationCanceledException"/> is swallowed as normal cancellation; other exceptions propagate.
    /// </summary>
    public class AsyncCommand : IAsyncCommand, INotifyPropertyChanged
    {
        private readonly Func<object, CancellationToken, Task> execute;
        private readonly Predicate<object> canExecute;
        private readonly bool allowConcurrentExecutions;
        private CancellationTokenSource cancellationSource;
        private bool isExecuting;
        private bool isCancellationRequested;
        private EventHandler _canExecuteChanged;

        public AsyncCommand(Func<CancellationToken, Task> execute, Func<bool> canExecute = null, bool allowConcurrentExecutions = false)
            : this(execute == null ? (Func<object, CancellationToken, Task>)null : (_, ct) => execute(ct),
                   canExecute == null ? (Predicate<object>)null : _ => canExecute(),
                   allowConcurrentExecutions)
        {
        }

        public AsyncCommand(Func<object, CancellationToken, Task> execute, Predicate<object> canExecute = null, bool allowConcurrentExecutions = false)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
            this.allowConcurrentExecutions = allowConcurrentExecutions;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler CanExecuteChanged
        {
            add { _canExecuteChanged += value; CommandManager.RequerySuggested += value; }
            remove { _canExecuteChanged -= value; CommandManager.RequerySuggested -= value; }
        }

        /// <summary>Whether an execution is currently in flight.</summary>
        public bool IsExecuting
        {
            get => isExecuting;
            private set
            {
                if (isExecuting == value) return;
                isExecuting = value;
                OnPropertyChanged(nameof(IsExecuting));
                RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Whether <see cref="Cancel"/> has been called against the current (or most recent)
        /// execution. Resets to <see langword="false"/> when a new execution starts.
        /// </summary>
        public bool IsCancellationRequested
        {
            get => isCancellationRequested;
            private set
            {
                if (isCancellationRequested == value) return;
                isCancellationRequested = value;
                OnPropertyChanged(nameof(IsCancellationRequested));
            }
        }

        public bool CanExecute(object parameter)
        {
            if (IsExecuting && !allowConcurrentExecutions) return false;
            return canExecute?.Invoke(parameter) ?? true;
        }

        public async void Execute(object parameter)
        {
            if (IsExecuting && !allowConcurrentExecutions) return;

            var source = new CancellationTokenSource();
            cancellationSource = source;
            IsCancellationRequested = false;
            IsExecuting = true;
            try
            {
                await execute(parameter, source.Token);
            }
            catch (OperationCanceledException)
            {
                // A canceled run is a normal completion for a cancelable command.
            }
            finally
            {
                if (ReferenceEquals(cancellationSource, source))
                {
                    cancellationSource = null;
                    IsExecuting = false;
                }
                source.Dispose();
            }
        }

        /// <summary>Requests cancellation of the in-flight execution (no-op when idle).</summary>
        public void Cancel()
        {
            var source = cancellationSource;
            if (source == null) return;
            IsCancellationRequested = true;
            source.Cancel();
        }

        public void RaiseCanExecuteChanged()
        {
            _canExecuteChanged?.Invoke(this, EventArgs.Empty);
            CommandManager.InvalidateRequerySuggested();
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
