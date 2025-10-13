using System;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Event arguments for value removal from filter tokens
    /// </summary>
    public class ValueRemovedFromTokenEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the ValueRemovedFromTokenEventArgs class
        /// </summary>
        /// <param name="removableToken">The removable token that should have its value removed</param>
        public ValueRemovedFromTokenEventArgs(IRemovableToken removableToken)
        {
            RemovableToken = removableToken ?? throw new ArgumentNullException(nameof(removableToken));
        }

        /// <summary>
        /// Gets the removable token that should have its value removed
        /// </summary>
        public IRemovableToken RemovableToken { get; }

        /// <summary>
        /// Gets the value token if the removable token is a ValueToken (for backward compatibility)
        /// </summary>
        public ValueToken ValueToken => RemovableToken as ValueToken;
    }
}