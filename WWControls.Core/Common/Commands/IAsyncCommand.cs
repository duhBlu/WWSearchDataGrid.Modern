using System.Windows.Input;

namespace WWControls.Core
{
    /// <summary>
    /// A cancelable, state-reporting async command. Controls that visualize asynchronous work
    /// (e.g. <c>WWButton</c>'s wait / wait-cancel display modes) test their bound
    /// <see cref="ICommand"/> for this interface: <see cref="IsExecuting"/> drives the busy
    /// indicator and <see cref="Cancel"/> backs the hover cancel affordance. Implementations should
    /// also raise <see cref="System.ComponentModel.INotifyPropertyChanged"/> for these properties —
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
