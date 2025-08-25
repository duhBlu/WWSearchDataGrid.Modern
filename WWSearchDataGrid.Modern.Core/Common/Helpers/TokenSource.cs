using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Helper class for managing cancellation tokens
    /// </summary>
    public class TokenSource
    {
        #region Fields

        private readonly List<CancellationTokenSource> _tokenSources = new List<CancellationTokenSource>();
        private readonly object _lock = new object();

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new cancellation token source and cancels existing ones
        /// </summary>
        /// <returns>The new cancellation token source</returns>
        public CancellationTokenSource GetNewCancellationTokenSource()
        {
            lock (_lock)
            {
                // Cancel all existing token sources
                foreach (var tokenSource in _tokenSources)
                {
                    if (!tokenSource.IsCancellationRequested)
                    {
                        tokenSource.Cancel();
                    }
                }

                // Create a new token source
                var newSource = new CancellationTokenSource();
                _tokenSources.Add(newSource);

                // Return the new source
                return newSource;
            }
        }

        /// <summary>
        /// Removes a cancellation token source from the collection
        /// </summary>
        /// <param name="source">The source to remove</param>
        public void RemoveCancellationTokenSource(CancellationTokenSource source)
        {
            lock (_lock)
            {
                _tokenSources.Remove(source);
            }
        }

        #endregion
    }
}
