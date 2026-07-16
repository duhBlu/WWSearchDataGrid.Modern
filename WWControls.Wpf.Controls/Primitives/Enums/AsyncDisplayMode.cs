namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// How a <see cref="WWButton"/> visualizes an asynchronous operation while
    /// <see cref="WWButton.IsAsyncOperationInProgress"/> is set (driven automatically when the
    /// bound command implements <see cref="WWControls.Core.IAsyncCommand"/>).
    /// </summary>
    public enum AsyncDisplayMode
    {
        /// <summary>The operation runs without any visual indication.</summary>
        None,

        /// <summary>The button shows a loading wheel for the duration of the operation.</summary>
        Wait,

        /// <summary>
        /// The button shows the loading wheel, and swaps it for a cancel ("×") affordance while the
        /// mouse hovers the button. Clicking then requests cancellation on the associated async
        /// command (its <c>IsCancellationRequested</c> flips to <see langword="true"/>) and raises
        /// <see cref="WWButton.CancelClickEvent"/> instead of a normal click.
        /// </summary>
        WaitCancel,
    }
}
