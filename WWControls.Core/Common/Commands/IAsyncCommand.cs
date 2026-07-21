using System.Windows.Input;

namespace WWControls.Core
{
    /// <summary>
    /// An <see cref="ICommand"/> that reports async execution state, so controls can visualize
    /// in-progress work: <see cref="IsExecuting"/> drives a busy indicator and <see cref="Cancel"/>
    /// backs a cancel affordance. Implementations should raise
    /// <see cref="System.ComponentModel.INotifyPropertyChanged"/> for these properties;
    /// <see cref="AsyncCommand"/> is the stock implementation.
    /// </summary>
    public interface IAsyncCommand : ICommand
    {
        /// <summary>Whether an execution is currently in flight.</summary>
        bool IsExecuting { get; }

        /// <summary>Whether cancellation has been requested for the current execution.</summary>
        bool IsCancellationRequested { get; }

        /// <summary>Requests cancellation of the in-flight execution (no-op when idle).</summary>
        void Cancel();
    }
}
