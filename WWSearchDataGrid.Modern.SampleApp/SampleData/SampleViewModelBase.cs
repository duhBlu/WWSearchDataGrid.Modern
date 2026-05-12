using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace WWSearchDataGrid.Modern.SampleApp.SampleData
{
    /// <summary>
    /// Base for sample view-models that load their data asynchronously after construction. The
    /// constructor stays cheap so the launcher can swap the sample view in immediately; data
    /// generation runs on a worker thread while <see cref="IsBusy"/> drives the loading overlay
    /// shown by the SampleHostControl template.
    /// </summary>
    public abstract partial class SampleViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _status = string.Empty;

        /// <summary>
        /// Flips IsBusy on, runs <paramref name="load"/>, and clears IsBusy when it completes.
        /// The continuation hops back to the original SynchronizationContext (the UI thread when
        /// invoked from a view-model constructor), so callers may assign UI-bound properties
        /// directly after their <c>await Task.Run(...)</c>.
        /// </summary>
        protected void StartLoad(Func<Task> load, string status = "Loading sample data…")
        {
            if (load == null) throw new ArgumentNullException(nameof(load));
            IsBusy = true;
            Status = status;
            _ = RunAsync(load);
        }

        private async Task RunAsync(Func<Task> load)
        {
            try { await load(); }
            finally
            {
                IsBusy = false;
                Status = string.Empty;
            }
        }
    }
}
